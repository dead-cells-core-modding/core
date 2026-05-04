namespace HaxeDocs
{
    public record class HaxeDocument
    {
        public record class TypeInfo
        {
            public string Name { get; set; } = "";
            public string Doc { get; set; } = "";
            public int Kind {
                get; set;
            }
            public List<MemberInfo> Members { get; set; } = [];
            public List<string> Inheritances { get; set; } = [];
        }
        public record class MemberInfo
        {
            public string Name { get; set; } = "";
            public string Doc { get; set; } = "";
            public bool IsFunction {
                get; set;
            }
            public bool IsStatic {
                get; set;
            }
        }
        public List<TypeInfo> Types { get; set; } = [];
    }
}
