using UnityEngine;
using System.Collections;

public class Gun : MonoBehaviour
{
	public GameObject joystickPrefab;
	public Joystick joystickRight = null;
	public float fireDelay = 0.5f;
	public float lastFireTm = 0;

	public Rigidbody2D rocket;				// Prefab of the rocket.
	public float speed = 20f;				// The speed the rocket will fire at.


	private PlayerControl playerCtrl;		// Reference to the PlayerControl script.
	private Animator anim;					// Reference to the Animator component.


	void Awake()
	{
		// Setting up the references.
		anim = transform.root.gameObject.GetComponent<Animator>();
		playerCtrl = transform.root.GetComponent<PlayerControl>();
		
		
		#if (UNITY_IPHONE || UNITY_ANDROID || UNITY_WP8 || UNITY_WP_8_1 || UNITY_BLACKBERRY || UNITY_TIZEN) && !UNITY_EDITOR
		if (joystickPrefab) {
			// Create left joystick
			GameObject go = Instantiate (joystickPrefab) as GameObject;
			go.name = "Joystick Right";
			joystickRight = go.GetComponent<Joystick> ();
		}
		#endif
	}

	void Start(){
		if(joystickRight!=null){
			GUITexture guiTex = joystickRight.GetComponent<GUITexture> ();
			guiTex.pixelInset = new Rect( Screen.width - guiTex.pixelInset.x - guiTex.pixelInset.width,
			                             guiTex.pixelInset.y, guiTex.pixelInset.width, guiTex.pixelInset.height);
		}
	}


	void Update ()
	{
		// If the fire button is pressed...
		bool isFire = false;

		if(joystickRight!=null)
			isFire = joystickRight.IsFingerDown() && (Time.time - lastFireTm) > fireDelay;
		else
			isFire = Input.GetButtonDown("Fire1");

		if(isFire){

			// ... set the animator Shoot trigger parameter and play the audioclip.
			anim.SetTrigger("Shoot");
			audio.Play();

			// If the player is facing right...
			if(playerCtrl.facingRight)
			{
				// ... instantiate the rocket facing right and set it's velocity to the right. 
				Rigidbody2D bulletInstance = Instantiate(rocket, transform.position, Quaternion.Euler(new Vector3(0,0,0))) as Rigidbody2D;
				bulletInstance.velocity = new Vector2(speed, 0);
			}
			else
			{
				// Otherwise instantiate the rocket facing left and set it's velocity to the left.
				Rigidbody2D bulletInstance = Instantiate(rocket, transform.position, Quaternion.Euler(new Vector3(0,0,180f))) as Rigidbody2D;
				bulletInstance.velocity = new Vector2(-speed, 0);
			}

			lastFireTm = Time.time;
		}
	}
}
