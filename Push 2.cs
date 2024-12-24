using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UI;
using TMPro;

public class BoxControllerWithoutDamping : MonoBehaviour
{
    public Rigidbody boxRb; // Reference to the box's Rigidbody
    public ActionBasedController controller; // Reference to the XR controller (this can be the right-hand controller for position reference)
    public float pushForce = 100f; // Initial force applied by the avatar
    public float maxDistance = 5f; // Maximum distance for applying force

    private bool isPushing = false;
    private Vector3 handPosition;

    public Slider pushForceSlider; // Reference to the UI Slider
    public TextMeshProUGUI pushForceText; // Display the current push force value
    public TextMeshProUGUI velocityText; // Display the object's velocity
    public Button resetButton; // Reference to the Reset Button UI

    [Header("Reset Settings")]
    public Vector3 resetPosition; // Position to reset the box to
    public Vector3 resetRotation; // Rotation to reset the box to (Euler angles)

    // XR Input devices for the left and right controllers
    private InputDevice leftHandDevice;
    private InputDevice rightHandDevice;

    void Start()
    {
        // Get the left-hand and right-hand controller devices
        leftHandDevice = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        rightHandDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

        // Initialize the Slider if assigned
        if (pushForceSlider != null)
        {
            pushForceSlider.minValue = 0f;
            pushForceSlider.maxValue = 100f; // Adjust max value as needed
            pushForceSlider.value = pushForce; // Set initial value
            pushForceSlider.onValueChanged.AddListener(UpdatePushForce);
        }

        // Initialize the push force text if assigned
        if (pushForceText != null)
        {
            pushForceText.text = $"{pushForce:F1}";
        }

        // Initialize the Reset Button
        if (resetButton != null)
        {
            resetButton.onClick.AddListener(ResetObjectPosition);
        }

        // If resetPosition is not set, use the current position
        if (resetPosition == Vector3.zero)
        {
            resetPosition = transform.position;
        }

        // If resetRotation is not set, use the current rotation
        if (resetRotation == Vector3.zero)
        {
            resetRotation = transform.eulerAngles;
        }
    }

    void Update()
    {
        // Update hand position from the controller's position (assuming this is the right-hand controller)
        handPosition = controller.transform.position;

        // Check if A button (primaryButton) is pressed on the right-hand controller
        bool isAButtonPressed = false;
        if (rightHandDevice.isValid)
        {
            rightHandDevice.TryGetFeatureValue(CommonUsages.primaryButton, out isAButtonPressed);
        }

        // Get joystick input from the left-hand controller
        Vector2 axisInput = Vector2.zero;
        if (leftHandDevice.isValid)
        {
            leftHandDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out axisInput);
        }

        // Conditions for pushing:
        // 1. A button (on the right controller) is pressed
        // 2. Joystick (on the left controller) is being moved
        // 3. Controller (right) is close enough to the object
        if (isAButtonPressed
            && axisInput.sqrMagnitude > 0.01f
            && Vector3.Distance(handPosition, boxRb.position) < maxDistance)
        {
            isPushing = true;
        }
        else
        {
            // If user stops pressing A or stops moving joystick, stop applying force.
            // The object will continue to glide due to its inertia.
            isPushing = false;
        }

        // Update the velocity text
        if (velocityText != null)
        {
            Vector3 velocity = boxRb.velocity;
            velocityText.text = $"{velocity.magnitude:F2} m/s";
        }
    }

    void FixedUpdate()
    {
        if (isPushing)
        {
            ApplyPushForce();
        }
        // If not pushing, the object glides naturally.
    }

    void ApplyPushForce()
    {
        // Get the joystick input again (to ensure correct direction in FixedUpdate)
        Vector2 axisInput = Vector2.zero;
        if (leftHandDevice.isValid)
        {
            leftHandDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out axisInput);
        }

        if (axisInput.sqrMagnitude > 0.01f)
        {
            // Determine push direction based on the joystick input from the left controller
            // Use the right-hand controller's orientation (since we're referencing controller for handPosition)
            Vector3 localDirection = new Vector3(axisInput.x, 0, axisInput.y).normalized;
            Vector3 pushDirection = controller.transform.TransformDirection(localDirection);

            boxRb.AddForce(pushDirection * pushForce, ForceMode.Force);
        }
        else
        {
            // If axis input vanished mid-frame, no force this frame.
            // The object continues to glide due to inertia.
        }
    }

    // Method to update push force when Slider value changes
    public void UpdatePushForce(float newPushForce)
    {
        pushForce = newPushForce;
        if (pushForceText != null)
        {
            pushForceText.text = $"{pushForce:F1}";
        }
    }

    // Method to reset the object's position and rotation
    public void ResetObjectPosition()
    {
        // Reset position and rotation
        boxRb.position = resetPosition;
        boxRb.rotation = Quaternion.Euler(resetRotation);

        // Reset velocity and angular velocity
        boxRb.velocity = Vector3.zero;
        boxRb.angularVelocity = Vector3.zero;

        // Reset velocity text
        if (velocityText != null)
        {
            velocityText.text = "0.00 m/s";
        }
    }
}
