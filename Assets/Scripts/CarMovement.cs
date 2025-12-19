using System;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.RuleTile.TilingRuleOutput;

[RequireComponent(typeof(Rigidbody2D))]
public class CarMovement : MonoBehaviour
{
    public float acceleration = 1f;
    public float deceleration = 0.5f;
    public float maxSteerSpeed = 2f;
    public float minSteerSpeed = 0.2f;
    public float brakeSpeed = 3f;
    private float curSteerSpeed = 0f;
    private Vector3 angle = Vector3.zero;
    private Vector2 carNormal;
    private float speed = 0f;
    private Quaternion rotate = Quaternion.identity;
    private new Rigidbody2D rigidbody2D;

    public enum Direction
    {
        Forward,
        Backward,
        Left,
        Right
    }

    void Start()
    {
        angle.z = transform.rotation.eulerAngles.z;
        rigidbody2D = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        carNormal = new Vector2(Mathf.Sin((-angle.z) * Mathf.Deg2Rad), Mathf.Cos((-angle.z) * Mathf.Deg2Rad));
        rotate.eulerAngles = angle;
        transform.rotation = rotate;


        //if (Input.GetKey(KeyCode.W))
        //{
        //    rigidbody2D.linearVelocity = GetImpulse(Direction.Forward);
        //}
        //if (Input.GetKey(KeyCode.S))
        //{
        //    rigidbody2D.linearVelocity = GetImpulse(Direction.Backward);
        //}
        //if (Input.GetKey(KeyCode.D))
        //{
        //    rigidbody2D.linearVelocity = GetImpulse(Direction.Right);
        //}
        //if (Input.GetKey(KeyCode.A))
        //{
        //    rigidbody2D.linearVelocity = GetImpulse(Direction.Left);
        //}

        if (curSteerSpeed > 0)
            curSteerSpeed -= 0.01f;

        if (speed > 0)
        {
            speed -= deceleration / 50;
            if (speed < 0.01)
                speed = 0;
        }
        if (speed < 0)
        {
            speed += deceleration / 50;
            if (speed > -0.01)
                speed = 0;
        }

    }

    public Vector2 GetImpulse(Direction direction)
    {
        //Forward and Backward Motion
        if (direction == Direction.Forward)
        {
            if (curSteerSpeed < maxSteerSpeed)
                curSteerSpeed += 0.035f;
            if (speed <= 0)
                speed = speed + acceleration / 15;
            else
                speed = speed + acceleration / 30;
        }
        else if (direction == Direction.Backward)
        {
            if (curSteerSpeed < maxSteerSpeed)
                curSteerSpeed += 0.035f;
            if (speed <= 0)
                speed = speed - deceleration / 15;
            else
                speed = Math.Max(speed - brakeSpeed / 15, 0);
        }

        //if (speed < 0.1 && speed > -0.1)
        //    curSteerSpeed = 0;

        //Steering Motion
        //if (speed != 0)
        //{
        if (direction == Direction.Right)
        {
            if (speed >= 0)
                angle += new Vector3(0, 0, Math.Min(-curSteerSpeed, -minSteerSpeed));
            if (speed < 0)
                angle += new Vector3(0, 0, Math.Max(curSteerSpeed, minSteerSpeed));
            rotate.eulerAngles = angle;
            transform.rotation = rotate;
        }
        else if (direction == Direction.Left)
        {
            if (speed >= 0)
                angle += new Vector3(0, 0, Math.Max(curSteerSpeed, minSteerSpeed));
            if (speed < 0)
                angle += new Vector3(0, 0, Math.Min(-curSteerSpeed, -minSteerSpeed));
            rotate.eulerAngles = angle;
            transform.rotation = rotate;
        }
        //}

        return carNormal * speed;
    }

    void OnCollisionEnter2D(Collision2D collider)
    {
        if (collider.gameObject.tag != null)
        {
            speed = -speed * 0.15f;
        }
    }

}