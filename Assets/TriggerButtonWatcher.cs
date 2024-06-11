using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;

[System.Serializable]
public class TriggerButtonEvent : UnityEvent<bool>{}

public class TriggerButtonWatcher : MonoBehaviour
{
    public TriggerButtonEvent triggerButtonPress;
    
    private bool lastButtonState = false;
    private List<UnityEngine.XR.InputDevice> allDevices;
    private List<UnityEngine.XR.InputDevice> devicesWithTriggerButton;
    
    void Start()
    {
        if (triggerButtonPress == null)
        {
            triggerButtonPress = new TriggerButtonEvent();
        }
        allDevices = new List<UnityEngine.XR.InputDevice>();
        devicesWithTriggerButton = new List<UnityEngine.XR.InputDevice>();
        InputTracking.nodeAdded += InputTracking_nodeAdded;
    }

    // check for new input devices when new XRNode is added
    private void InputTracking_nodeAdded(XRNodeState obj)
    {
        updateInputDevices();
    }

    void Update()
    {
        Debug.Log("update");
        bool tempState = false;
        bool invalidDeviceFound = false;
        foreach(var device in devicesWithTriggerButton)
        {
            Debug.Log(device.name);
            bool triggerButtonState = false;
            tempState = device.isValid // the device is still valid
                        && device.TryGetFeatureValue(CommonUsages.triggerButton, out triggerButtonState) // did get a value
                        && triggerButtonState // the value we got
                        || tempState; // cumulative result from other controllers
            if (!device.isValid)
                invalidDeviceFound = true;
        }

        if (tempState != lastButtonState) // Button state changed since last frame
        {
            triggerButtonPress.Invoke(tempState);
            lastButtonState = tempState;
        }

        if (invalidDeviceFound || devicesWithTriggerButton.Count == 0) // refresh device lists
            updateInputDevices();
    }

    // find any devices supporting the desired feature usage
    void updateInputDevices()
    {
        devicesWithTriggerButton.Clear();
        UnityEngine.XR.InputDevices.GetDevices(allDevices);
        Debug.Log(allDevices.Count);
        bool discardedValue;
        foreach (var device in allDevices)
        {
            if(device.TryGetFeatureValue(CommonUsages.triggerButton, out discardedValue))
            {
                devicesWithTriggerButton.Add(device); // Add any devices that have a primary button.
            }
        }
    }
}