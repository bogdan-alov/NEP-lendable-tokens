using Neo.SmartContract.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace TransfairExpiration
{
    public class Events : SmartContract
    {
		public delegate void deleLend(byte[] from, byte[] to, byte[] value, BigInteger duration);
		[DisplayName("lend")]
		public static event deleLend Lend;
		public static void RaiseLend(byte[] from, byte[] to, byte[] value, BigInteger duration) => Lend(from, to, value, duration);

		public delegate void deleTransfer(byte[] from, byte[] to, byte[] value);
        [DisplayName("transfer")]
        public static event deleTransfer Transferred;
        public static void RaiseTransfer(byte[] from, byte[] to, byte[] value) => Transferred(from, to, value);

		public delegate void deleApproved(byte[] owner, byte[] approved, BigInteger tokenId);
		[DisplayName("approve")]
		public static event deleApproved Approved;
        public static void RaiseApproved(byte[] owner, byte[] approved, BigInteger tokenId) => Approved(owner, approved, tokenId);
    }
}
