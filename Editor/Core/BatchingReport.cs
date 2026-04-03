using System.Collections.Generic;
using UnityEngine;

namespace BatchSight.Core
{
    public enum BatchingStatus { Green, Yellow, Red }

    public class BatchingReport
    {
        public Renderer renderer;
        public BatchingStatus status = BatchingStatus.Yellow;
        public List<BatchingIssue> issues = new List<BatchingIssue>();
        public Bounds bounds;
        public int lastHash = 0;

        public string Name => renderer != null ? renderer.gameObject.name : "<null>";

        public BatchingReport(Renderer r)
        {
            renderer = r;
            if (r != null) bounds = r.bounds;
        }
    }
}
