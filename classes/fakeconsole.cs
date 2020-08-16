using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace donniebot.classes
{
    public class FakeConsole : StringWriter
    {
        public FakeConsole(StringBuilder builder) : base(builder) { }

        public void Beep() { }

        public void Beep(int a, int b) { }

        public void Clear() { }

        public void MoveBufferArea(int a, int b, int c, int d, int e) { }

        public void MoveBufferArea(int a, int b, int c, int d, int e, char f, ConsoleColor g, ConsoleColor h) { }

        public Stream OpenStandardError() => new MemoryStream();

        public Stream OpenStandardError(int a) => new MemoryStream(a);

        public Stream OpenStandardInput() => new MemoryStream();

        public Stream OpenStandardInput(int a) => new MemoryStream(a);

        public Stream OpenStandardOutput() => new MemoryStream();

        public Stream OpenStandardOutput(int a) => new MemoryStream(a);

        public int Read() => 0;

        public ConsoleKeyInfo ReadKey() => new ConsoleKeyInfo('p', ConsoleKey.P, false, false, false);

        public ConsoleKeyInfo ReadKey(bool p)
        {
            if (p)
            {
                Write("p");
            }
            return ReadKey();
        }

        public string ReadLine() => $"p{Environment.NewLine}";

        public void ResetColor() { }

        public void SetBufferSize(int a, int b) { }

        public void SetCursorPosition(int a, int b) { }

        public void SetError(TextWriter wr) { }

        public void SetIn(TextWriter wr) { }

        public void SetOut(TextWriter wr) { }

        public void SetWindowPosition(int a, int b) { }

        public void SetWindowSize(int a, int b) { }
    }
}