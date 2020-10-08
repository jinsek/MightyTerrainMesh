using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MTViewer : MonoBehaviour
{
    public float Yaw = 8f;
    public float Height = 2f;
    public float MoveSpeed = 1f;
    public float RotateSpeed = 1f;
    private byte mMoveFlag = 0;
    private RaycastHit mHit = new RaycastHit();
    private MTLoader mLoader;
    private Vector3 mUp;
    private void Awake()
    {
        mLoader = GetComponent<MTLoader>();
        mUp = Quaternion.Euler(Yaw, 0, 0) * Vector3.up;
    }
    // Start is called before the first frame update
    void Start()
    {
        if (Physics.Raycast(transform.position + 1000f * Vector3.up, Vector3.down, out mHit, float.MaxValue))
        {
            transform.position = mHit.point + Vector3.up * Height;
            Vector3 f = transform.forward;
            f.y = 0;
            transform.rotation = Quaternion.LookRotation(f, mUp);
        }
    }

    // Update is called once per frame
    void Update()
    {
        //fix height
        if (Physics.Raycast(transform.position + 1000f * Vector3.up, Vector3.down, out mHit, float.MaxValue))
        {
            if (mHit.point.y - transform.position.y > 2f)
            {
                transform.position = mHit.point + Vector3.up * 2f;
            }
        }
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            mMoveFlag |= 0x01;
        }
        if (Input.GetKeyUp(KeyCode.W) || Input.GetKeyUp(KeyCode.UpArrow))
        {
            mMoveFlag &= 0xfe;
        }
        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            mMoveFlag |= 0x02;
        }
        if (Input.GetKeyUp(KeyCode.S) || Input.GetKeyUp(KeyCode.DownArrow))
        {
            mMoveFlag &= 0xfd;
        }
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            mMoveFlag |= 0x04;
        }
        if (Input.GetKeyUp(KeyCode.D) || Input.GetKeyUp(KeyCode.RightArrow))
        {
            mMoveFlag &= 0xfb;
        }
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            mMoveFlag |= 0x08;
        }
        if (Input.GetKeyUp(KeyCode.A) || Input.GetKeyUp(KeyCode.LeftArrow))
        {
            mMoveFlag &= 0xf7;
        }
        if (mMoveFlag > 0)
        {
            Vector3 delta = Vector3.zero;
            Quaternion q = Quaternion.identity;
            if ((mMoveFlag & 0x01) > 0)
                delta += Vector3.forward;
            if ((mMoveFlag & 0x02) > 0)
                delta += Vector3.back;
            if ((mMoveFlag & 0x04) > 0)
                q *= Quaternion.Euler(0, RotateSpeed * Time.deltaTime, 0);
            if ((mMoveFlag & 0x08) > 0)
                q *= Quaternion.Euler(0, -RotateSpeed * Time.deltaTime, 0);
            transform.position += MoveSpeed * transform.TransformVector(delta * Time.deltaTime);
            transform.rotation *= q;
            if (Physics.Raycast(transform.position + 1000f * Vector3.up, Vector3.down, out mHit, float.MaxValue))
            {
                transform.position = mHit.point + Vector3.up * Height;
                Vector3 f = transform.forward;
                f.y = 0;
                transform.rotation = Quaternion.LookRotation(f, mUp);
            }
            if (mLoader != null)
                mLoader.SetDirty();
        }
    }
}
