#include "Runtime.hpp"

#include <telco-core.h>

namespace Telco
{
  volatile int Runtime::refCount = 0;

  void Runtime::Ref ()
  {
    g_atomic_int_inc (&refCount);
    telco_init ();
  }

  void Runtime::Unref ()
  {
    if (g_atomic_int_dec_and_test (&refCount))
    {
      telco_deinit ();
    }
  }

  class Assembly
  {
  public:
    Assembly ()
    {
      Runtime::Ref ();
    }

    ~Assembly ()
    {
      Runtime::Unref ();
    }
  };
  static Assembly assembly;
}