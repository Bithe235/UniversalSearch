using System.Collections.Generic;

namespace BatchSight.Core
{
    public static class BatchSightState
    {
        public static List<BatchingReport> Reports = new List<BatchingReport>();
        public static event System.Action OnReportsUpdated;

        public static void NotifyUpdated()
        {
            OnReportsUpdated?.Invoke();
        }
    }
}
