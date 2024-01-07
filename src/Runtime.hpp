#pragma once

namespace Telco
{
  class Runtime
  {
  public:
    static void Ref ();
    static void Unref ();

  private:
    static volatile int refCount;
  };
}