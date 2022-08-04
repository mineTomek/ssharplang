using System;

namespace S_Sharp
{
    class Program
    {
        static void Main(string[] args)
        {
            while (true) {
                Console.Write("s# > ");
                string code = Console.ReadLine();

                if (code == "") { continue; }

                Result res = ssharp.Run("<stdin>", code);

                Console.WriteLine(res.ToString());
            }
        }
    }

    public class Result
    {
        public ssharp.Error error;
        public string text;

        public Result(ssharp.Error _error, string _text)
        {
            error = _error;
            text = _text;
        }

        public override string ToString()
        {
            if (error == null)
            {
                return text;
            } else
            {
                return error.ToString();
            }
        }
    }
}
