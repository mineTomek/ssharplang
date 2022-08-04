using System;
namespace S_Sharp
{
    public class Arrows
    {
        public string arrowsText;

        public Arrows(string text, ssharp.Position posStart, ssharp.Position posEnd)
        {
            string result = "";

            // Calculate indices
            int idxStart = Math.Max(text.LastIndexOf("\n", 0, posStart.idx), 0);
        }
    }
}
