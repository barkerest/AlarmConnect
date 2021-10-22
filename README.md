# AlarmConnect

A somewhat generic API library for accessing alarm.com accounts.

### Why?

Well my dealer doesn't have me on an enterprise package for some reason and I have
9 alarm systems I manage.  I need to switch between multiple systems to locate users.

So I wrote this library to automate the tasks of logging in, switching systems,
inventorying users, enumerating access, and linking email addresses to users for auditing
purposes.

### Warning!

Alarm.com does not publish their API and can freely change the API specs at any time.
As an example, they changed their login mechanism sometime earlier this year which
broke the previous iteration of this library.  Because they have no obligation to notify
us end users about these changes, there is no guarantee that this library will work.

### Usage

With that out of the way, usage is relatively simple.

```c#
ILogger logger;
ILoginCredentials credentials;

var session = new AlarmConnect.Session(credentials, logger);

// handle MFA requests (at least using an authenticator app)
session.MfaRequired += (sender, e) => {
    var mfaCode = GetMfaCodeFromUser();
    var success = (sender as AlarmConnect.Session).VerifyTwoFactorViaAuthenticatorApp(mfaCode).Result;
    Debug.Assert(success);
};

if (!session.Login().Result)
{
    // handle the login failure.
}

// get list of available systems.
var availableSystems = session.GetAvailableSystems();

foreach (var availableSystem in availableSystems)
{
    session.SelectSystem(availableSystem.Id);
    
    // load users and their email addresses.
    var users = session.GetUsers().With(x => x.EmailAddresses);
    
    // do something with user list.
}
```

I clearly did not cover every endpoint or action.  Luckily, thanks to the consistency of the 
API so far, new endpoints can be added by implementing the IDataObjectFillable interface on
new types.

```c#
class MyClass : IDataObjectFillable
{
    ...
}

var myClassItem = session.GetOne<MyClass>(id);
var myClassItems = session.GetMany<MyClass>(ids);
```

Raw access to the API is provided using the ApiGet and ApiPost methods of the session interface.


### License

Copyright (C) 2021 Beau Barker (beau@barkerest.com)

Released under the [MIT License](https://opensource.org/licenses/MIT).

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.