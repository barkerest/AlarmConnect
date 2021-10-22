using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using AlarmConnect;
using AlarmConnect.Models;
using Microsoft.Extensions.Logging;
using NPOI.HSSF.Util;
using NPOI.SS.UserModel;
using Sample.Excel;

namespace Sample
{
    class Program
    {
        static string ReadMaskedLine()
        {
            var result = new StringBuilder();

            while (true)
            {
                var keyInfo = Console.ReadKey(true);

                if (keyInfo.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    return result.ToString();
                }

                if (keyInfo.Key == ConsoleKey.Backspace &&
                    result.Length > 0)
                {
                    result.Remove(result.Length - 1, 1);
                    Console.Write("\b \b");
                }
                else if (!char.IsControl(keyInfo.KeyChar))
                {
                    result.Append(keyInfo.KeyChar);
                    Console.Write('*');
                }
            }
        }

        static void Main(string[] args)
        {
            var log = new ConsoleLogger();

            Console.WriteLine(" Alarm.com Connection Sample");
            Console.WriteLine("=============================");
            Console.WriteLine();
            Console.WriteLine("This program will connect to alarm.com to list systems, partitions, and users.");
            Console.WriteLine("Users are grouped together to create unique users.");
            Console.WriteLine("Email addresses are synced up within unique users.");
            Console.WriteLine("The extracted data can be written to an XLSX file for your enjoyment.");
            Console.WriteLine();
            Console.Write("Enter your user name: ");
            var loginUser = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(loginUser))
            {
                log.LogWarning("No user name entered, assuming you want to quit.");
                return;
            }

            Console.Write("Enter your password: ");
            var pwd = ReadMaskedLine();
            if (string.IsNullOrEmpty(pwd))
            {
                log.LogWarning("No password entered, assuming you want to quit.");
                return;
            }

            var creds = new Credentials() { UserName = loginUser, Password = pwd };
            using (var session = new AlarmConnect.Session(creds, log.CreateLogger<AlarmConnect.Session>()))
            {
                session.MfaRequired += SessionOnMfaRequired;
                if (!session.Login().Result)
                {
                    return;
                }

                log.LogInformation("Loading systems...");
                var availSystems = session.GetAvailableSystems();
                var systems = session.GetSystems(availSystems.Select(x => x.Id).ToArray())
                                     .Select(
                                         y =>
                                         {
                                             session.SelectSystem(y.Id);
                                             return y
                                                    .With(x => x.Partitions)
                                                    .With(x => x.Sensors);
                                         }
                                     )
                                     .ToArray();


                log.LogInformation($"Loaded {systems.Length} systems with a total of {systems.Sum(x => x.Partitions.Count)} partitions and {systems.Sum(x => x.Sensors.Count)} sensors.");

                var users = systems.SelectMany(
                                       sys =>
                                       {
                                           session.SelectSystem(sys.Id);
                                           var ret = session.GetUsers()
                                                            .With(x => x.EmailAddresses)
                                                            .With(x => x.DeviceAccesses);

                                           ret.SelectMany(x => x.DeviceAccesses)
                                              .With(x => x.AccessPointCollections)
                                              .SelectMany(x => x.AccessPointCollections)
                                              .With(x => x.Partitions);

                                           return ret;
                                       }
                                   )
                                   .ToArray();

                log.LogInformation($"Loaded {users.Length} users.");

                var uniqueUsers = users
                                  .SelectMany(
                                      y => y.DeviceAccesses
                                            .Select(
                                                z => new
                                                {
                                                    Code         = z.UserCode,
                                                    FirstName    = y.FirstName,
                                                    LastName     = y.LastName,
                                                    Email        = y.EmailAddresses.FirstOrDefault(z => z.Enabled)?.Address,
                                                    PartitionIds = z.AccessPointCollections.SelectMany(w => w.PartitionIds),
                                                    UniqueId     = $"{z.UserCode} {y.FirstName.Trim()} {y.LastName.Trim()}".ToUpper(),
                                                    y.LoadedFromSystemId,
                                                    y.Id
                                                }
                                            )
                                  )
                                  .GroupBy(x => x.UniqueId)
                                  .OrderBy(x => x.Key)
                                  .Select(
                                      x => new
                                      {
                                          Code        = x.First().Code,
                                          FirstName   = x.First().FirstName,
                                          LastName    = x.First().LastName,
                                          Email       = x.FirstOrDefault(y => !string.IsNullOrWhiteSpace(y.Email))?.Email ?? "",
                                          PartionIds  = x.SelectMany(y => y.PartitionIds).Distinct().ToArray(),
                                          SystemNames = x.Select(y => systems.FirstOrDefault(z => z.Id == y.LoadedFromSystemId)?.Name ?? "???").Distinct().ToArray(),
                                          Ids         = x.Select(y => y.Id).Distinct().ToArray()
                                      }
                                  )
                                  .ToArray();

                foreach (var uniqueUser in uniqueUsers.Where(x => !string.IsNullOrWhiteSpace(x.Email)))
                {
                    foreach (var user in uniqueUser.Ids.Select(x => users.First(y => y.Id == x)))
                    {
                        if (!user.EmailAddresses.Any(x => x.Address.Equals(uniqueUser.Email, StringComparison.OrdinalIgnoreCase)))
                        {
                            if (session.AddEmailToUser(user, uniqueUser.Email, false) is null)
                            {
                                log.LogWarning($"Failed to add email to {user.Name}.");
                            }
                        }
                    }
                }

                log.LogInformation($"Identified {uniqueUsers.Length} unique users.");


                Console.WriteLine();
                Console.WriteLine("Now you can save this information to an XLSX workbook, or press enter to exit.");
                Console.Write("Save as: ");
                var path = Console.ReadLine()?.Trim();

                if (string.IsNullOrEmpty(path)) return;

                try
                {
                    using (var fileStream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        log.LogInformation($"Saving workbook to \"{path}\"...");
                        var book = new StandardWorkbook();

                        book.AddSheet(
                            "Systems",
                            systems,
                            system => system.Id,
                            system => system.Name,
                            system => system.UnitId
                        );

                        var partitions = systems.SelectMany(x => x.Partitions.Select(y => new AlarmPartition(y, x))).ToArray();

                        book.AddSheet(
                            "Partitions",
                            partitions,
                            x => x.Id,
                            x => x.Name
                        );

                        book.AddSheet(
                            "Sensors",
                            systems.SelectMany(x => x.Sensors.Select(y => new AlarmSensor(y, x))),
                            x => x.Id,
                            x => x.Name,
                            x => x.State
                        );

                        var strikeStyle = book.Workbook.CreateCellStyle();
                        strikeStyle.SetFont(book.RecordFont);
                        strikeStyle.Alignment           = HorizontalAlignment.Left;
                        strikeStyle.VerticalAlignment   = VerticalAlignment.Bottom;
                        strikeStyle.DataFormat          = book.Workbook.CreateDataFormat().GetFormat("@");
                        strikeStyle.FillForegroundColor = HSSFColor.Yellow.Index;
                        strikeStyle.FillPattern         = FillPattern.SolidForeground;

                        book.BuildSheet(
                            "Users",
                            sheet =>
                            {
                                sheet.SetHeaderRow(
                                    row =>
                                    {
                                        row.AppendCell("Code");
                                        row.AppendCell("First Name");
                                        row.AppendCell("Last Name");
                                        row.AppendCell("Email");
                                        foreach (var part in partitions)
                                        {
                                            row.AppendCell(part.Name);
                                        }

                                        row.AppendCell("Found in Systems");
                                    }
                                );

                                foreach (var u in uniqueUsers)
                                {
                                    sheet.AppendRow(
                                        row =>
                                        {
                                            var c = row.AppendCell(u.Code);

                                            if (!u.PartionIds.Any()) c.CellStyle = strikeStyle;
                                            c = row.AppendCell(u.FirstName);
                                            if (!u.PartionIds.Any()) c.CellStyle = strikeStyle;
                                            c = row.AppendCell(u.LastName);
                                            if (!u.PartionIds.Any()) c.CellStyle = strikeStyle;
                                            c = row.AppendCell(u.Email);
                                            if (!u.PartionIds.Any()) c.CellStyle = strikeStyle;

                                            foreach (var part in partitions)
                                            {
                                                var hasAccess = u.PartionIds.Contains(part.Id) ? "YES" : "";
                                                row.AppendCell(hasAccess, HorizontalAlignment.Center);
                                            }

                                            c = row.AppendCell(string.Join(", ", u.SystemNames));
                                        }
                                    );
                                }
                            }
                        );


                        book.Workbook.Write(fileStream);
                        log.LogDebug("Workbook saved.");
                    }
                }
                catch (IOException e)
                {
                    log.LogError(e, "Failed to create test file.");
                }
                catch (UnauthorizedAccessException)
                {
                    log.LogError($"Cannot write to \"{path}\".");
                }
                catch (NotSupportedException)
                {
                    log.LogError($"The path \"{path}\" is not in a valid format.");
                }
            }
        }

        private static void SessionOnMfaRequired(object sender, EventArgs e)
        {
            var session = sender as Session;
            if (session is null) return;

            Console.WriteLine();
            Console.Write("Enter authenticator app code: ");
            var code = Console.ReadLine()?.Trim() ?? "";

            if (!session.VerifyTwoFactorViaAuthenticatorApp(code).Result)
            {
                Console.WriteLine("ERROR: Failed to verify MFA code.");
                return;
            }

            Console.WriteLine("MFA verified.");
        }
    }
}
