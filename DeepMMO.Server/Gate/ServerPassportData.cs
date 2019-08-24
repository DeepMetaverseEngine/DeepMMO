using System;
using System.Collections.Generic;
using System.Text;

namespace DeepMMO.Server.Gate
{
    public class ServerPassportData
    {
        private readonly bool verified;

        private readonly byte privilege;

        public ServerPassportData(bool verified, byte privilege)
        {
            this.verified = verified;
            this.privilege = privilege;
        }
        public bool Verified => verified;

        public byte Privilege => privilege;
    }
}
