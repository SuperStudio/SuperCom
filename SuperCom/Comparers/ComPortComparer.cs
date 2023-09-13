using System.Collections.Generic;

namespace SuperCom.Comparers
{
    public class ComPortComparer : IComparer<string>
    {
        public int Compare(string portNameA, string portNameB)
        {
            string a = portNameA.Replace("COM", "");
            string b = portNameB.Replace("COM", "");
            int.TryParse(a, out int va);
            int.TryParse(b, out int vb);
            return va - vb;
        }
    }
}
