# NE (Netly Encoding)
``NE`` is a ``class`` it is a ``System.Text.Encoding`` wrapper

## Properties
- Mode ``enum`` <br>
    <sub>Encoding protocols</sub>
    ```
    enum Mode { ASCII = 0, UTF8 = 1, UNICODE = 2 }
    ```

- Defaut ``NE.Mode`` <br>
    <sub>Default (explicitly) and global encoding and decoding ``default value is UTF8``</sub>
    ```cs
    NE.Default = NE.Mode.ASCII;
    string sampleString = NE.GetString(new byte[] {0, 1, 2, 3}); // NE.Mode is ASCII

    NE.Default = NE.Mode.UTF8;
    string utfString = NE.GetString(new byte[] {0, 1, 2, 3}); // NE.Mode is UTF8
    ```


## Methods (Static)
- GetString ``string(byte[] buffer)`` ``string(byte[] buffer, NE.Mode mode)`` <br>
    <sub>Convert string to bytes (byte[])</sub>
    ```cs
    // NE.Mode is NE.Default (global)
    string defaultEncoding = NE.GetString(new byte[] { 0, 1, 2, 3 });

    string utf8String = NE.GetString(new byte[] { 0, 1, 2, 3 }, NE.Mode.UTF8);
    string asciiString = NE.GetString(new byte[] { 0, 1, 2, 3 }, NE.Mode.ASCII);
    string unicodeString = NE.GetString(new byte[] { 0, 1, 2, 3 }, NE.Mode.UNICODE);
    ```
- GetBytes ``byte[](string buffer)`` ``byte[](string buffer, NE.Mode mode)`` <br>
    <sub>Convert bytes (byte[]) to string</sub>
    ```cs
    // NE.Mode is NE.Default (global)
    byte[] defaultEncoding = NE.GetBytes("😅 Alecio is funny");

    byte[] utf8Bytes = NE.GetBytes("😅 Alecio is funny", NE.Mode.UTF8);
    byte[] asciiBytes = NE.GetBytes("😅 Alecio is funny", NE.Mode.ASCII);
    byte[] unicodeBytes = NE.GetBytes("😅 Alecio is funny", NE.Mode.UNICODE);
    ```