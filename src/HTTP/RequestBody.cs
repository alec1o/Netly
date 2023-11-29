using System;
using Netly.Core;

namespace Netly
{
    public class RequestBody
    {
        public readonly byte[] Buffer;
        public readonly int Length;
        public KeyValueContainer Form => GetForm();
        public string PlainText => GetPlainText();

        public RequestBody(byte[] bodyBuffer)
        {
            Buffer = bodyBuffer ?? Array.Empty<byte>();
            Length = Buffer.Length;
        }

        private string _plainText = string.Empty;
        private bool _initPlainText = false;

        private string GetPlainText()
        {
            if (_initPlainText is false)
            {
                _plainText = NE.GetString(Buffer, NE.Mode.UTF8);
                _initPlainText = true;
            }

            return _plainText;
        }

        private KeyValueContainer _form = null;
        private bool _initForm = false;

        private KeyValueContainer GetForm()
        {
            if (_initForm is false)
            {
                _form = new KeyValueContainer();
                // TODO: SEARCH Add element on form
                _initForm = true;
            }

            return _form;
        }
    }
}