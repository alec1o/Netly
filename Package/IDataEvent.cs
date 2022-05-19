using System;
using System.Collections.Generic;

namespace Zenet.Package
{
    public interface IDataEvent
    {
        #region Init

        string[] Errors { get; }

        #endregion

        #region Add

        // integer numbers
        void Add(byte value);
        void Add(int value);
        void Add(uint value);
        void Add(short value);
        void Add(ushort value);
        void Add(long value);
        void Add(ulong value);

        // real numbers  
        void Add(float value);
        void Add(decimal value);
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

        #endregion        
        
        #region Get

        // integer numbers
        void Get(byte value);
        void Get(int value);
        void Get(uint value);
        void Get(short value);
        void Get(ushort value);
        void Get(long value);
        void Get(ulong value);

        // real numbers  
        void Get(float value);
        void Get(decimal value);
        void Get(double value);

        // text
        void Get(string value);
        void Get(char value);
        void Get(bool value);

        // others
        void Get(byte[] value);
        void Get(Vec3 value);
        void Get(Vec2 value);
        void Get(DateTime value);             

        #endregion
    }
}
