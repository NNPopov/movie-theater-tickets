using System.Security.Cryptography;
using System.Text;

namespace CinemaTicketBooking.Domain.ShoppingCarts.Abstractions;

public interface IDataHasher
{
    string ComputeHash(string input);
}

public class DataHasher:IDataHasher
{
    public string ComputeHash(string input)
    {
        StringBuilder sb = new StringBuilder();

        using (MD5 md5 = MD5.Create())
        {
            byte[] hashValue = md5.ComputeHash(Encoding.UTF8.GetBytes(input));

            foreach (byte b in hashValue)
            {
                sb.Append($"{b:X2}");
            }
        }

        return sb.ToString();
    }
}

