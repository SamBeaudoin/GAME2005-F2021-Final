using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CollisionManager : MonoBehaviour
{
    public CubeBehaviour[] cubes;
    public BulletBehaviour[] bullets;

    private static Vector3[] faces;

    // Start is called before the first frame update
    void Start()
    {
        cubes = FindObjectsOfType<CubeBehaviour>();

        faces = new Vector3[]
        {
            Vector3.left, Vector3.right,
            Vector3.down, Vector3.up,
            Vector3.back , Vector3.forward
        };
    }

    // Update is called once per frame
    void Update()
    {
        bullets = FindObjectsOfType<BulletBehaviour>();

        // check each AABB with every other AABB in the scene
        for (int i = 0; i < cubes.Length; i++)
        {
            for (int j = 0; j < cubes.Length; j++)
            {
                if (i != j)
                {
                    CheckAABBs(cubes[i], cubes[j]);
                }
            }
        }

        // Check each bullet against each AABB in the scene
        foreach (var bullet in bullets)
        {
            foreach (var cube in cubes)
            {
                if (cube.name != "Player")
                {
                    CheckBulletAABB(bullet, cube);
                }
                
            }
        }


    }

    public static void CheckBulletAABB(BulletBehaviour bullet, CubeBehaviour cube)
    {
        // get box closest point to sphere center by clamping
        var x = Mathf.Max(cube.min.x, Mathf.Min(bullet.transform.position.x, cube.max.x));
        var y = Mathf.Max(cube.min.y, Mathf.Min(bullet.transform.position.y, cube.max.y));
        var z = Mathf.Max(cube.min.z, Mathf.Min(bullet.transform.position.z, cube.max.z));

        var distance = Math.Sqrt((x - bullet.transform.position.x) * (x - bullet.transform.position.x) +
                                 (y - bullet.transform.position.y) * (y - bullet.transform.position.y) +
                                 (z - bullet.transform.position.z) * (z - bullet.transform.position.z));

        if ((distance < bullet.halfMax) && (!bullet.isColliding))
        {
            // determine the distances between the contact extents
            float[] distances = {
                (cube.max.x - bullet.transform.position.x),
                (bullet.transform.position.x - cube.min.x),
                (cube.max.y - bullet.transform.position.y),
                (bullet.transform.position.y - cube.min.y),
                (cube.max.z - bullet.transform.position.z),
                (bullet.transform.position.z - cube.min.z)
            };

            float penetration = float.MaxValue;
            Vector3 face = Vector3.zero;

            // check each face to see if it is the one that connected
            for (int i = 0; i < 6; i++)
            {
                if (distances[i] < penetration)
                {
                    // determine the penetration distance
                    penetration = distances[i];
                    face = faces[i];
                }
            }

            bullet.penetration = penetration;
            bullet.collisionNormal = face;
            //s.isColliding = true;

            
            Reflect(bullet);
        }

    }
    
    // This helper function reflects the bullet when it hits an AABB face
    private static void Reflect(BulletBehaviour s)
    {
        if ((s.collisionNormal == Vector3.forward) || (s.collisionNormal == Vector3.back))
        {
            s.direction = new Vector3(s.direction.x, s.direction.y, -s.direction.z);
        }
        else if ((s.collisionNormal == Vector3.right) || (s.collisionNormal == Vector3.left))
        {
            s.direction = new Vector3(-s.direction.x, s.direction.y, s.direction.z);
        }
        else if ((s.collisionNormal == Vector3.up) || (s.collisionNormal == Vector3.down))
        {
            s.direction = new Vector3(s.direction.x, -s.direction.y, s.direction.z);
        }
    }


    public static void CheckAABBs(CubeBehaviour a, CubeBehaviour b)
    {
        Contact contactB = new Contact(b);

        if ((a.min.x <= b.max.x && a.max.x >= b.min.x) &&
            (a.min.y <= b.max.y && a.max.y >= b.min.y) &&
            (a.min.z <= b.max.z && a.max.z >= b.min.z))
        {
            // determine the distances between the contact extents
            float[] distances = {
                (b.max.x - a.min.x),
                (a.max.x - b.min.x),
                (b.max.y - a.min.y),
                (a.max.y - b.min.y),
                (b.max.z - a.min.z),
                (a.max.z - b.min.z)
            };

            float penetration = float.MaxValue;
            Vector3 face = Vector3.zero;

            // check each face to see if it is the one that connected
            for (int i = 0; i < 6; i++)
            {
                if (distances[i] < penetration)
                {
                    // determine the penetration distance
                    penetration = distances[i];
                    face = faces[i];
                }
            }
            
            // set the contact properties
            contactB.face = face;
            contactB.penetration = penetration;


            // check if contact does not exist
            if (!a.contacts.Contains(contactB))
            {
                // remove any contact that matches the name but not other parameters
                for (int i = a.contacts.Count - 1; i > -1; i--)
                {
                    if (a.contacts[i].cube.name.Equals(contactB.cube.name))
                    {
                        a.contacts.RemoveAt(i);
                    }
                }

                if (contactB.face == Vector3.down)
                {
                    a.gameObject.GetComponent<RigidBody3D>().Stop();
                    a.isGrounded = true;
                }
                

                // add the new contact
                a.contacts.Add(contactB);
                a.isColliding = true;

                if (a.name != "Player")
                {
                    // Call Translation Function to move cubes
                    ApplyTranslation(a, contactB);
                }
            }
        }
        else
        {

            if (a.contacts.Exists(x => x.cube.gameObject.name == b.gameObject.name))
            {
                a.contacts.Remove(a.contacts.Find(x => x.cube.gameObject.name.Equals(b.gameObject.name)));
                a.isColliding = false;

                if (a.gameObject.GetComponent<RigidBody3D>().bodyType == BodyType.DYNAMIC)
                {
                    a.gameObject.GetComponent<RigidBody3D>().isFalling = true;
                    a.isGrounded = false;
                }
            }
        }
    }

    public static void ApplyTranslation(CubeBehaviour a, Contact c)
    {
        // Find normal and penetration
        Vector3 ContactVector = c.face;
        float penetration = c.penetration;

        // Find Translation Vector
        Vector3 TranslationVectorA = -ContactVector * penetration;

        // If on ground and collision is above
        if(c.face == Vector3.up  && a.isGrounded)
        {
            return;
        }

        // Move Object
        if(!(a.gameObject.GetComponent<RigidBody3D>().bodyType == BodyType.STATIC)) 
        {
            a.transform.Translate(TranslationVectorA);
        }

    }
}
