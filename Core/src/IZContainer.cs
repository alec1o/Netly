namespace Zenet.Core
{
    public interface IZContainer
    {
        /// <summary>
        /// Is the deserialization index
        /// </summary>
        int Index { get; }

        /// <summary>
        /// It is the result of serialization
        /// </summary>
        byte[] Serialized { get; }

        /// <summary>
        /// Data that will be used to deserialize
        /// </summary>
        byte[] Deserialize { set; }

        /// <summary>
        /// Contains all deserialize error
        /// </summary>
        string[] Errors { get; }

        #region Add

        // integer numbers
        void AddByte(byte value);
        void AddInt(int value);
        void AddShort(short value);
        void AddLong(long value);

        // real numbers  
        void AddFloat(float value);
        void AddDouble(double value);

        // text
        void AddString(string value);
        void AddChar(char value);
        void AddBool(bool value);

        // others
        void AddBytes(byte[] value);
        void AddVector2(ZVector2 value);
        void AddVector3(ZVector3 value);

        #endregion

        #region Get

        byte GetByte();
        int GetInt();
        short GetShort();
        long GetLong();

        // real numbers  
        float GetFloat();
        double GetDouble();

        // text
        string GetString();
        char GetChar();
        bool GetBool();

        // others
        byte[] GetBytes();
        ZVector2 GetVector2();
        ZVector3 GetVector3();

        #endregion
    }
}
