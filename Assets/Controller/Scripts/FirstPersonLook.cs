using UnityEngine;
using Mirror;
public class FirstPersonLook : MonoBehaviour
{
    ////[SerializeField]
    ////Transform character;
    public Transform spine;
    public Transform body;
    public float sensitivity = 2;
    public float smoothing = 1.5f;
    FirstPersonMovement movement;
    Vector2 velocity;
    Vector2 frameVelocity;
    Vector2 current_mouse_look;
    Vector2 look_angles;
    void Reset()
    {
        // Get the character from the FirstPersonMovement in parents.
        ////character = GetComponentInParent<FirstPersonMovement>().transform;
    }

    void Start()
    {
        // Lock the mouse cursor to the game screen.
        Cursor.lockState = CursorLockMode.Locked;
        movement = GetComponentInParent<FirstPersonMovement>();
    }

    void Update()
    {
       
        
            // Get smooth velocity.
            Vector2 mouseDelta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
            Vector2 rawFrameVelocity = Vector2.Scale(mouseDelta, Vector2.one * sensitivity);
            frameVelocity = Vector2.Lerp(frameVelocity, rawFrameVelocity, 1 / smoothing);
            velocity += frameVelocity;
            velocity.y = Mathf.Clamp(velocity.y, -100, 45);

        // Rotate camera up-down and controller left-right from velocity.
        ////transform.localRotation = Quaternion.AngleAxis(-velocity.y, Vector3.right);
        
        ////character.localRotation = Quaternion.AngleAxis(velocity.x, Vector3.up);
        ////body.transform.rotation = Quaternion.AngleAxis(velocity.x, Vector3.up);
        ////spine.transform.rotation = Quaternion.AngleAxis(-velocity.y, Vector3.right);
        //spine.transform.rotation = Quaternion.Euler(transform.localRotation.x,velocity.y,transform.localRotation.z);
        current_mouse_look = new Vector2(Input.GetAxis("Mouse Y"), Input.GetAxis("Mouse X"));   //Gde se mis nalazi
        look_angles.x += current_mouse_look.x * sensitivity;  //Kako se rotira kamera po x osi da dodje do misa
        look_angles.y += current_mouse_look.y * sensitivity;  //Kako se rotira kamera po y osi da dodje do misa
        look_angles.x = Mathf.Clamp(look_angles.x, -100, 45);
        spine.localRotation = Quaternion.Euler(-look_angles.x, transform.localRotation.y,0f);    //Kako da se rotiraju kamere
        body.localRotation = Quaternion.Euler(0f, -look_angles.y, 180f);    //Kako da se rotira igrac

    }
}
