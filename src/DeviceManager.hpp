#pragma once

#using <WindowsBase.dll>

#include <telco-core.h>
#include <msclr/gcroot.h>

using namespace System;
using System::Windows::Threading::Dispatcher;

namespace Telco
{
  ref class Device;

  public ref class DeviceManager
  {
  public:
    DeviceManager (Dispatcher ^ dispatcher);
    ~DeviceManager ();
  protected:
    !DeviceManager ();

  public:
    event EventHandler ^ Changed;

    array<Device ^> ^ EnumerateDevices ();

  internal:
    void OnChanged (Object ^ sender, EventArgs ^ e);

  private:
    TelcoDeviceManager * handle;
    msclr::gcroot<DeviceManager ^> * selfHandle;

    Dispatcher ^ dispatcher;
    EventHandler ^ onChangedHandler;
  };
}