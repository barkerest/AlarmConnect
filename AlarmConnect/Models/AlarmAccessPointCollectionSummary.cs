using System;
using System.Collections.Generic;
using System.Linq;

namespace AlarmConnect.Models
{
    public class AlarmAccessPointCollectionSummary : IDataObjectFillable
    {
        private class AccessPointItem
        {
            public int  DeviceId  { get; set; }
            public bool HasAccess { get; set; }
        }

        private class AccessPointCollection
        {
            public AccessPointItem[] AccessPointItems { get; set; }
        }

        public string Id                 { get; private set; }
        public bool   CannotUseSchedules { get; private set; }
        public bool   IsAllAccessUser    { get; private set; }

        public IReadOnlyList<AlarmPartition> Partitions   { get; private set; } = null;
        public IReadOnlyList<string>         PartitionIds { get; private set; }

        string IDataObjectFillable.AcceptedApiType
            => "users/access/access-point-collections-summary";

        string IDataObjectFillable.ApiEndpoint
            => "users/access/accessPointCollectionsSummaries";

        private ISession             _session;
        ISession IDataObjectFillable.Session => _session;

        void IDataObjectFillable.FillFromDataObject(IDataObject dataObject, ISession session)
        {
            if (dataObject is null) throw new ArgumentNullException(nameof(dataObject));
            if (dataObject.ApiType != ((IDataObjectFillable)this).AcceptedApiType) throw new ArgumentException("Data is not of the correct type.");
            _session = session ?? throw new ArgumentNullException(nameof(session));

            Id                 = dataObject.Id;
            CannotUseSchedules = dataObject.GetBooleanAttribute("cannotUseSchedules");
            IsAllAccessUser    = dataObject.GetBooleanAttribute("isAllAccessUser");

            var coll     = dataObject.GetAttributeAs<Dictionary<string, AccessPointCollection[]>>("groupsAccessPointCollections");
            var systemId = _session.GetSelectedSystem();
            if (coll.ContainsKey(systemId))
            {
                var unitId = session.GetSelectedUnitId();
                PartitionIds = coll[systemId]
                               .SelectMany(x => x.AccessPointItems)
                               .Where(x => x.HasAccess)
                               .Select(x => $"{unitId}-{x.DeviceId}")
                               .ToArray();
            }
            else
            {
                PartitionIds = Array.Empty<string>();
            }
        }
    }
}
