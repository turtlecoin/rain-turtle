using System.Threading.Tasks;

namespace TurtleBot.Services
{
    public class TurtleWallet
    {
        public string Address { get; }

        private TurtleWallet(string address)
        {
            Address = address;
        }

        public static async Task<TurtleWallet> FromString(WalletService walletService, string address)
        {
            if (!address.StartsWith("TRTL")) return null;
            if (address.Length != 99) return null;
            if (!await walletService.CheckAddress(address)) return null;

            return new TurtleWallet(address);
        }
    }
}