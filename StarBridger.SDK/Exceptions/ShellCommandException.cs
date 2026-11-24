using System;
using System.Collections.Generic;
using System.Text;

namespace StarBridger.SDK.Exceptions;

public class ShellCommandException : IOException
{
  internal ShellCommandException(string message) : base(message)
  {

  }
}
