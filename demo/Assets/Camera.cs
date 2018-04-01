using UnityEngine;
using System.Collections;
using Assets.Map;

public class Camera : MonoBehaviour
{
    public Map Map;
    float _mousePosX;
    float _mousePosY;
    float _scrollSpeed = 0.2f;
    float _zoomSpeed = 1f;
    Vector2 _mouseLeftClick;

    void Update()
    {
        float deltaX = Input.mousePosition.x - _mousePosX;
        float deltaY = Input.mousePosition.y - _mousePosY;
        
        _mousePosX = Input.mousePosition.x;
        _mousePosY = Input.mousePosition.y;

        if (Input.GetMouseButton(0))
        {
            Ray ray = GetComponent<UnityEngine.Camera>().ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                _mouseLeftClick = hit.point;
                Map.Click(_mouseLeftClick);
            }
        }

        if (Input.GetMouseButton(1))
        {
            var newX = Mathf.Clamp(transform.parent.position.x + deltaX * _scrollSpeed, 0, Map.Width);
            var newY = Mathf.Clamp(transform.parent.position.y + deltaY * _scrollSpeed, 0, Map.Height);

            transform.parent.position = new Vector3(newX, newY, transform.parent.position.z);
        }

        if (Input.GetAxis("Mouse ScrollWheel") < 0 && transform.parent.localPosition.z > -20)
        {
            transform.parent.Translate(new Vector3(0, 0, -_zoomSpeed));
        }

        if (Input.GetAxis("Mouse ScrollWheel") > 0 && transform.parent.localPosition.z < -5)
        {
            transform.parent.Translate(new Vector3(0, 0, _zoomSpeed));
        }
    }
}
