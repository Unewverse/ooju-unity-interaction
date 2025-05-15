using System.Collections.Generic;

namespace OojuInteractionPlugin
{
    public static class CAIGAnalyzer
    {
        // Represents a node in the scene object hierarchy
        public class ObjectNode
        {
            public string name;
            public string type;
            public List<ObjectNode> children;
        }

        // Represents the result of a scene analysis
        public class AnalysisData
        {
            public string scene_name;
            public List<ObjectNode> root_objects;
        }
    }
} 