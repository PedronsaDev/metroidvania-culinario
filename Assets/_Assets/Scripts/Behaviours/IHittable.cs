using UnityEngine;

public interface IHittable
{
    public bool GiveUpwardForce { get; set; }
    public bool WasHit { get; set; }
    public float UpwardForce { get; set; }

    public void Hit(Vector3 hitPoint, Vector3 hitDirection, int damage = 1);

    public Transform GetTransform();
}
