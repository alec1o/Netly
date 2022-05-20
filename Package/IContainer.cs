using System;
using System.Collections.Generic;

namespace Zenet.Package
{
    public interface IContainer
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
        void Add(byte value);
        void Add(int value);
        void Add(short value);
        void Add(long value);

        // real numbers  
        void Add(float value);
        void Add(double value);

        // text
        void Add(string value);
        void Add(char value);
        void Add(bool value);

        // others
        void Add(byte[] value);
        void Add(Vec3 value);
        void Add(Vec2 value);
        void Add(DateTime value);

        #endregion#region Add

        #region Get

        // integer numbers
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
        Vec3 GetVec3();
        Vec2 GetVec2();
        DateTime GetDateTime();

        #endregion
    }
}
