using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using System;
using System.Numerics;

namespace TransfairExpiration
{

    public class TokenOwnerExpiration : SmartContract
    {
        public static readonly byte[] ContractOwner = "AK2nJJpJr6o664CWJKi1QRXjqeic2zRp8y".ToScriptHash();

        public static object Main(string operation, params object[] args)
        {
            if (operation == "version")
            {
                return Version();
            }
            else if (operation == "name")
            {
                return Name();
            }
            else if (operation == "mintToken")
            {
                if (args.Length != 1)
                {
                    return false;
                }
                byte[] owner = (byte[])args[0];

                return MintToken(owner);
            }
            else if (operation == "symbol")
            {
                return Symbol();
            }
            else if (operation == "totalSupply")
            {
                return TotalSupply();
            }
            else if (operation == "decimals")
            {
                return 0;
            }
            else if (operation == "ownerOf")
            {
                return OwnerOf((byte[])args[0]);
            }
            else if (operation == "tokenURI")
            {
                BigInteger tokenId = (BigInteger)args[0];

                return TokenURI(tokenId);
            }
            else if (operation == "balanceOf")
            {
                if (args.Length != 1)
                {
                    return 0;
                }

                byte[] account = (byte[])args[0];

                return BalanceOf(account);
            }
            else if (operation == "tokensOfOwner")
            {
                byte[] owner = (byte[])args[0];

                return TokensOfOwner(owner);
            }
            else if (operation == "transfer")
            {
                if (args.Length != 3)
                {
                    return false;
                }

                byte[] from = (byte[])args[0];
                byte[] to = (byte[])args[1];
                byte[] tokenId = (byte[])args[2];

                if (!Runtime.CheckWitness(from))
                {
                    return false;
                }

                var callingScript = ExecutionEngine.CallingScriptHash;
                if (ExecutionEngine.EntryScriptHash.AsBigInteger() != callingScript.AsBigInteger())
                {
                    return false;
                }

                return Transfer(from, to, tokenId);
            }
            else if (operation == "lend")
            {
                if (args.Length < 3 || args.Length > 4)
                {
                    return false;
                }

                byte[] from = (byte[])args[0];
                byte[] to = (byte[])args[1];
                byte[] tokenId = (byte[])args[2];

                if (!Runtime.CheckWitness(from))
                {
                    return false;
                }

                var callingScript = ExecutionEngine.CallingScriptHash;
                if (ExecutionEngine.EntryScriptHash.AsBigInteger() != callingScript.AsBigInteger())
                {
                    return false;
                }
                BigInteger duration = 9999999999999999;
                if (args.Length == 4)
                {
                    duration = (BigInteger)args[3];
                }

                return Lend(from, to, tokenId, duration);
            }
            else if (operation == "isLendActive")
            {
                if (args.Length != 1)
                {
                    return false;
                }
                return IsLendActive((byte[])args[0]);

            }
            else if (operation == "returnToOwner")
            {
                if (args.Length != 1)
                {
                    return false;
                }

                byte[] tokenId = (byte[])args[0];
                bool isActive = IsLendActive(tokenId);
                Runtime.Notify("return is active", isActive);
                if (!isActive)
                {
                    TokenInfo token = DataAccess.GetToken(tokenId);

                    byte[] owner = OwnerOf(tokenId);
                    Runtime.Notify("origownerreturn", token.OriginalOwner, owner);
                    if (Runtime.CheckWitness(token.Owner) || Runtime.CheckWitness(token.OriginalOwner))
                        Transfer(owner, token.OriginalOwner, tokenId);
                }
            }
            return false;
        }

        static bool IsLendActive(byte[] tokenId)
        {
            TokenInfo token = DataAccess.GetToken(tokenId);
          
            var nowtime = Blockchain.GetHeader(Blockchain.GetHeight()).Timestamp;
            if (nowtime > token.LendExpiration || token.Owner == token.OriginalOwner)
            {
                return false;
            }
            return true;
        }
        /// <summary>
        /// Total amount
        /// </summary>
        public const ulong MaxSupply = 4320;

        /// <summary>
        /// Attepts to handle the current operation
        /// </summary>


        private static string Name() => "BreedOrBrawl";

        /// <summary>
        /// Symbol
        /// </summary>
        private static string Symbol() => "BOB";

        /// <summary>
        /// Version
        /// </summary>
        private static string Version() => "0.0.1";

        /// <summary>
        /// Get the total number of tokens that have been issued
        /// </summary>
        private static BigInteger TotalSupply() => DataAccess.GetTotalSupplyAsBytes().AsBigInteger();

        private static string TokenURI(BigInteger tokenId) => "uri/" + tokenId;

        private static BigInteger BalanceOf(byte[] owner) =>
            Storage.Get(Storage.CurrentContext, Keys.AddressBalanceKey(owner)).AsBigInteger();

        /// <summary>
        /// Get the token owner address
        /// </summary>
        private static byte[] OwnerOf(byte[] tokenIdAsByteArray)
        {
            TokenInfo token = DataAccess.GetToken(tokenIdAsByteArray);
            if (token != null && token.Owner.Length > 0)
            {
                return token.Owner;
            }
            else
            {
                return new byte[] { };
            }
        }

        private static BigInteger[] TokensOfOwner(byte[] owner)
        {
            BigInteger tokenCount = BalanceOf(owner);
            BigInteger[] result = new BigInteger[(int)tokenCount];

            if (tokenCount == 0)
            {
                return result;
            }
            else
            {
                // We count on the fact that all NFTInfo have IDs starting at 1 and increasing
                // sequentially up to the totalCat count.
                for (BigInteger idx = 1; idx < tokenCount + 1; idx += 1)
                {
                    var tokenId = DataAccess.GetOwnersTokenIdByIndexAsBytes(owner, idx);
                    result[(int)idx - 1] = tokenId.AsBigInteger();
                }

                return result;
            }
        }

        /// <summary>
        /// Generate new token data and record
        /// </summary>
        public static BigInteger MintToken(byte[] owner)
        {
            if (owner.Length != 20)
            {
                return 0;
            }

            if (!Runtime.CheckWitness(ContractOwner))
            {
                Runtime.Log("Only the contract owner may mint new tokens.");
                return 0;
            }

            //TODO: Determine whether the total amount is exceeded
            byte[] tokenaId = Storage.Get(Storage.CurrentContext, Keys.KeyAllSupply);

            byte[] tokenId = DataAccess.GetTotalSupplyAsBytes();
            BigInteger nextTokenId = tokenId.AsBigInteger() + 1;
            tokenId = nextTokenId.AsByteArray();

            TokenInfo token = new TokenInfo();
            token.Owner = owner;
            //Used to check if lend is possible
            token.OriginalOwner = token.Owner;
            //Blockchain.GetHeader(Blockchain.GetHeight()).Timestamp;

            DataAccess.SetToken(tokenId, token);
            DataAccess.SetTotalSupply(tokenId);

            BigInteger index = DataAccess.IncreaseAddressBalance(owner);
            DataAccess.SetTokenOfOwnerAtIndex(owner, index, nextTokenId);

            return tokenId.AsBigInteger();
        }


        /// <summary>
        /// Transfer the token ownership
        /// </summary>
        public static bool Transfer(byte[] from, byte[] to, byte[] tokenId)
        {
            if (from.Length != 20 || to.Length != 20)
            {
                return false;
            }

            if (from == to)
            {
                return true;
            }

            var token = DataAccess.GetToken(tokenId);

            if (from != token.Owner)
            {
                return false;
            }

            token.Owner = to;

            DataAccess.SetToken(tokenId, token);
            DataAccess.RemoveApproval(tokenId);

            var index = DataAccess.IncreaseAddressBalance(to);
            DataAccess.SetTokenOfOwnerAtIndex(to, index, tokenId.AsBigInteger());

            var currentBalance = DataAccess.DecreaseAddressBalance(from);
            DataAccess.ShiftLastTokenOfOwnerToTransferedTokenIndex(from, tokenId.AsBigInteger(), currentBalance + 1);

            Events.RaiseTransfer(from, to, tokenId);

            return true;
        }


        public static bool Lend(byte[] from, byte[] to, byte[] tokenId, BigInteger expiration)
        {
            if (from.Length != 20 || to.Length != 20)
            {
                return false;
            }
            //if (from == to)
            //{
            //    return true;
            //}

            var token = DataAccess.GetToken(tokenId);
            Runtime.Notify("token", token.OriginalOwner);
            if (from != token.Owner)
            {
                return false;
            }
            Runtime.Notify("check witness");

            if (!Runtime.CheckWitness(token.OriginalOwner))
                return false;

            var nowtime = Blockchain.GetHeader(Blockchain.GetHeight()).Timestamp;
            Runtime.Notify("now", nowtime);
            token.LendExpiration = expiration + nowtime;
            token.Owner = to;
            Runtime.Notify("done", token.Owner, to);
            Runtime.Notify("lendExpiration", token.LendExpiration);

            DataAccess.SetToken(tokenId, token);
            
            var index = DataAccess.IncreaseAddressBalance(to);
            DataAccess.SetTokenOfOwnerAtIndex(to, index, tokenId.AsBigInteger());

            var currentBalance = DataAccess.DecreaseAddressBalance(from);
            DataAccess.ShiftLastTokenOfOwnerToTransferedTokenIndex(from, tokenId.AsBigInteger(), currentBalance + 1);

            //Events.RaiseLend(from, to, tokenId, expiration);

            return true;
        }
    }
}
