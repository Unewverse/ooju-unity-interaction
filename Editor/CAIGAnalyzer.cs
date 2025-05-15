using UnityEngine;
using System.Collections.Generic;

namespace OojuInteractionPlugin
{
    public static class CAIGAnalyzer
    {
        public class ObjectNode
        {
            public string name;
            public string type;
            public List<ObjectNode> children;
        }

        public class AnalysisData
        {
            public string scene_name;
            public List<ObjectNode> root_objects;
        }
    }
} 