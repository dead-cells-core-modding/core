namespace NonPublicNativeMembers
{
    public class NativeMembersData
    {
        public class MemberInfo
        {
            public string Name
            {
                get; set;
            } = "";
            public string ModuleName
            {
                get; set;
            } = "";
            public ulong RVA
            {
                get; set;
            }
            public bool IsFunction
            {
                get; set;
            }
        }
        public class ModuleInfo
        {
            public string Name
            {
                get; set;
            } = "";
            public byte[] Hash
            {
                get; set;
            } = [];
            public Dictionary<string, MemberInfo> Members
            {
                get; set;
            } = [];
        }
        public List<ModuleInfo> Modules
        {
            get; set;
        } = [];
    }
}
