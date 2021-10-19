#region

using System;
using System.IO;
using System.Text;
using UnityEngine;

#endregion

/// <summary>
///     Redirects writes to System.Console to Unity3D's Debug.Log.
/// </summary>
/// <author>
///     Jackson Dunstan, http://jacksondunstan.com/articles/2986
/// </author>
public static class UnitySystemConsoleRedirector
{
    #region 02. Actions

    public static void Redirect()
    {
        Console.SetOut(new UnityTextWriter());
    }

    #endregion

    #region 07. Nested Types

    private class UnityTextWriter : TextWriter
    {
        #region 02. Actions

        public override void Flush()
        {
            Debug.Log(_buffer.ToString());
            _buffer.Length = 0;
        }

        public override void Write(string value)
        {
            _buffer.Append(value);
            if (value != null)
            {
                var len = value.Length;
                if (len > 0)
                {
                    var lastChar = value[len - 1];
                    if (lastChar == '\n') Flush();
                }
            }
        }

        public override void Write(char value)
        {
            _buffer.Append(value);
            if (value == '\n') Flush();
        }

        public override void Write(char[] value, int index, int count)
        {
            Write(new string(value, index, count));
        }

        #endregion

        #region 04. Public variables

        public override Encoding Encoding => Encoding.Default;

        #endregion

        #region 05. Private variables

        private readonly StringBuilder _buffer = new StringBuilder();

        #endregion
    }

    #endregion
}