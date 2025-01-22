using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Proxy.Objects
{
    public interface IHashlinkFieldObject : IHashlinkPointer
    {
        object? GetFieldValue(int hashedName);
        object? GetFieldValue(string name);
        void SetFieldValue(int hashedName, object? value);
        void SetFieldValue(string name, object? value);
    }
}
