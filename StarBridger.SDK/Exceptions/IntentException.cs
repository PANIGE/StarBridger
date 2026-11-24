using System;
using System.Collections.Generic;
using System.Text;

namespace StarBridger.SDK.Exceptions;

public class IntentException : InvalidOperationException
{
  internal IntentException(string message) : base(message) { }
}
