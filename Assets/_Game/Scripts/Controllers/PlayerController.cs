using BecomingLegend.Actors;
using UnityEngine;

namespace BecomingLegend.Controllers
{
    public class PlayerController : MonoBehaviour
    {
        private PlayerActor player;
        private Rigidbody2D rb;
        private Animator animator;
        private Vector2 lastDirection = Vector2.down;
        private Vector2 inputBuffer;

        private float runMultiplier = 2f;
        private float doubleTapTime = 0.3f;

        private KeyCode lastDirKey;
        private float lastDirPressTime;
        private bool isRunning;

        private void Awake()
        {
            player = GetComponent<PlayerActor>();
            rb = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();
        }

        private void Update()
        {
            bool pressedW = Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);
            bool pressedS = Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow);
            bool pressedA = Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow);
            bool pressedD = Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow);

            float h = 0f, v = 0f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) v = 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) v = -1f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) h = -1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) h = 1f;

            if (Input.GetKeyDown(KeyCode.Z))
                player.Attack();

            if (pressedW) HandleDirPress(KeyCode.W);
            else if (pressedS) HandleDirPress(KeyCode.S);
            else if (pressedA) HandleDirPress(KeyCode.A);
            else if (pressedD) HandleDirPress(KeyCode.D);

            if (h == 0f && v == 0f)
                isRunning = false;

            inputBuffer = new Vector2(h, v).normalized;

            if (inputBuffer.magnitude > 0.01f)
                lastDirection = inputBuffer;

            animator.SetFloat("MoveX", lastDirection.x);
            animator.SetFloat("MoveY", lastDirection.y);
            animator.SetFloat("Speed", inputBuffer.magnitude > 0.01f ? (isRunning ? 1f : 0.3f) : 0f);
        }

        private void FixedUpdate()
        {
            float currentSpeed = player.MoveSpeed * (isRunning ? runMultiplier : 1f);
            Vector2 targetPos = rb.position + inputBuffer * currentSpeed * Time.fixedDeltaTime;
            rb.MovePosition(targetPos);
        }

        private void HandleDirPress(KeyCode key)
        {
            if (key == lastDirKey && Time.time - lastDirPressTime < doubleTapTime)
                isRunning = true;
            else
                isRunning = false;
            lastDirKey = key;
            lastDirPressTime = Time.time;
        }
    }
}
