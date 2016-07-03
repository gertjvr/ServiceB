using ServiceA.MessageContracts;

namespace ServiceB
{
    class Program
    {
        static void Main(string[] args)
        {
            var command = new SendCommand();
        }
    }
}
