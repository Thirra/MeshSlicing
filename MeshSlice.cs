using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshSlice : MonoBehaviour
{
    #region Sword Variables
    public Transform top;
    public Transform bottom;

    private Vector3 swordPosition; //Might not need this.
    private Vector3 swordTopPosition;
    private Vector3 swordBottomPosition;
    private Vector3 colliderPosition;
    #endregion

    #region Triangle Struct
    struct Triangle
    {
        public Vector3 v1;
        public Vector3 v2;
        public Vector3 v3;

        public Vector3 GetNormal()
        {
            return Vector3.Cross(v1 - v2, v1 - v3).normalized;
        }

        //Flips direction if it doessn't match with Vector3 direction
        public void MatchDirection(Vector3 direction)
        {
            if (Vector3.Dot(GetNormal(), direction) > 0)
            {
                return;
            }
            else
            {
                Vector3 vs1 = v1;
                v1 = v3;
                v3 = vs1;
            }
        }
    }
    #endregion

    private GameObject mesh;
    public GameObject sender;

    public void Start()
    {
        mesh = GameObject.FindGameObjectWithTag("Mesh");
    }

    public void Update()
    {
        sender = transform.root.gameObject;
    }

    /// <summary>
    /// Getting the top and bottom positions of the "sword" swing on a trigger 
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        swordTopPosition = top.position;
        swordBottomPosition = bottom.position;

        if (other.transform.root.gameObject != sender.transform.root.gameObject && other.gameObject.tag == "Player")
        {
            other.GetComponent<Health>().TakeDamage(20);
        }
    }

    /// <summary>
    /// If the collider is sliceable, do the slicey thing
    /// </summary>
    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "Sliceable")
        {
            colliderPosition = transform.position;

            //This is the plane that I wanted to create - debugging for reference
            Debug.DrawLine(swordTopPosition, swordBottomPosition, Color.red);
            Debug.DrawLine(swordBottomPosition, colliderPosition, Color.red);
            Debug.DrawLine(colliderPosition, swordTopPosition, Color.red);

            //Creating the plane of the slice
            Plane slicePlane = new Plane(swordTopPosition, swordBottomPosition, colliderPosition);

            //Setting target objects values
            Transform targetTransform = other.gameObject.transform;
            Mesh targetMesh = other.gameObject.GetComponent<MeshFilter>().mesh;
            if (targetMesh == null)
                targetMesh = other.gameObject.GetComponentInChildren<MeshFilter>().mesh;
            int[] targetTriangles = targetMesh.triangles;
            Vector3[] targetVertices = targetMesh.vertices;

            //Storing data
            List<Vector3> intersections = new List<Vector3>();
            List<Triangle> newTriangles1 = new List<Triangle>();
            List<Triangle> newTriangles2 = new List<Triangle>();

            //Loop through all the triangles in the mesh
            for (int index = 0; index < targetTriangles.Length; index += 3)
            {
                List<Vector3> points = new List<Vector3>();

                int v1 = targetTriangles[index];
                int v2 = targetTriangles[index + 1];
                int v3 = targetTriangles[index + 2];

                //Vertex's in world coordinates
                Vector3 vertex1 = targetTransform.TransformPoint(targetVertices[v1]);
                Vector3 vertex2 = targetTransform.TransformPoint(targetVertices[v2]);
                Vector3 vertex3 = targetTransform.TransformPoint(targetVertices[v3]);

                //Normal of the triangle 
                Vector3 normal = Vector3.Cross(vertex1 - vertex2, vertex1 - vertex3);

                Vector3 direction = vertex2 - vertex1;
                float value;
                if (slicePlane.Raycast(new Ray(vertex1, direction), out value) && value <= direction.magnitude)
                {
                    Vector3 intersection = vertex1 + value * direction.normalized;
                    intersections.Add(intersection);
                    points.Add(intersection);
                }

                direction = vertex3 - vertex2;
                if (slicePlane.Raycast(new Ray(vertex2, direction), out value) && value <= direction.magnitude)
                {
                    Vector3 intersection = vertex2 + value * direction.normalized;
                    intersections.Add(intersection);
                    points.Add(intersection);
                }

                direction = vertex3 - vertex1;
                if (slicePlane.Raycast(new Ray(vertex1, direction), out value) && value <= direction.magnitude)
                {
                    Vector3 intersection = vertex1 + value * direction.normalized;
                    intersections.Add(intersection);
                    points.Add(intersection);
                }

                //If an intersection was found, subdivide and add 3 triangles
                if (points.Count > 0)
                {
                    //This makes it not work??
                    if (points.Count != 2)
                        return;

                    //Vertices for the first slice
                    List<Vector3> points1 = new List<Vector3>();
                    //Add the intersections vertices (they are shared for both sides)
                    points1.AddRange(points);

                    List<Vector3> points2 = new List<Vector3>();
                    points2.AddRange(points);

                    //Check on which side each of the original vertices were
                    if (slicePlane.GetSide(vertex1))
                    {
                        points1.Add(vertex1);
                    }
                    else
                    {
                        points2.Add(vertex1);
                    }

                    if (slicePlane.GetSide(vertex2))
                    {
                        points1.Add(vertex2);
                    }
                    else
                    {
                        points2.Add(vertex2);
                    }

                    if (slicePlane.GetSide(vertex3))
                    {
                        points1.Add(vertex3);
                    }
                    else
                    {
                        points2.Add(vertex3);
                    }

                    if (points1.Count == 3)
                    {
                        Triangle pTriangle = new Triangle() { v1 = points1[1], v2 = points1[0], v3 = points1[2] };
                        pTriangle.MatchDirection(normal);
                        newTriangles1.Add(pTriangle);
                    }
                    else
                    {
                        if (Vector3.Dot((points1[0] - points1[1]), points1[2] - points1[3]) >= 0)
                        {
                            Triangle pTriangle = new Triangle() { v1 = points1[0], v2 = points1[2], v3 = points1[3] };
                            pTriangle.MatchDirection(normal);
                            newTriangles1.Add(pTriangle);

                            pTriangle = new Triangle() { v1 = points1[0], v2 = points1[3], v3 = points1[1] };
                            pTriangle.MatchDirection(normal);
                            newTriangles1.Add(pTriangle);
                        }
                        else
                        {
                            Triangle pTriangle = new Triangle() { v1 = points1[0], v2 = points1[3], v3 = points1[2] };
                            pTriangle.MatchDirection(normal);
                            newTriangles1.Add(pTriangle);

                            pTriangle = new Triangle() { v1 = points1[0], v2 = points1[2], v3 = points1[1] };
                            pTriangle.MatchDirection(normal);
                            newTriangles1.Add(pTriangle);
                        }
                    }

                    //Second slice
                    if (points2.Count == 3)
                    {
                        Triangle pTriangle = new Triangle() { v1 = points2[1], v2 = points2[0], v3 = points2[2] };
                        pTriangle.MatchDirection(normal);
                        newTriangles2.Add(pTriangle);
                    }
                    else
                    {
                        if (Vector3.Dot((points2[0] - points2[1]), points2[2] - points2[3]) >= 0)
                        {
                            Triangle pTriangle = new Triangle() { v1 = points2[0], v2 = points2[2], v3 = points2[3] };
                            pTriangle.MatchDirection(normal);
                            newTriangles2.Add(pTriangle);

                            pTriangle = new Triangle() { v1 = points2[0], v2 = points2[3], v3 = points2[1] };
                            pTriangle.MatchDirection(normal);
                            newTriangles2.Add(pTriangle);
                        }
                        else
                        {
                            Triangle pTriangle = new Triangle() { v1 = points2[0], v2 = points2[3], v3 = points2[2] };
                            pTriangle.MatchDirection(normal);
                            newTriangles2.Add(pTriangle);

                            pTriangle = new Triangle() { v1 = points2[0], v2 = points2[2], v3 = points2[1] };
                            pTriangle.MatchDirection(normal);
                            newTriangles2.Add(pTriangle);
                        }
                    }
                }
                //If no intersection found, add the original triangle
                else
                {
                    //Check which side of the plane it is on
                    if (slicePlane.GetSide(vertex1))
                    {
                        newTriangles1.Add(new Triangle() { v1 = vertex1, v2 = vertex2, v3 = vertex3 });
                    }
                    else
                    {
                        newTriangles2.Add(new Triangle() { v1 = vertex1, v2 = vertex2, v3 = vertex3 });
                    }
                }
            }

            //Generating new geometry
            if (intersections.Count > 1)
            {
                Vector3 center = Vector3.zero;
                foreach (Vector3 vector in intersections)
                {
                    center += vector;
                }
                center /= intersections.Count;
                for (int index = 0; index < intersections.Count; index++)
                {
                    Triangle triangle = new Triangle() { v1 = intersections[index], v2 = center, v3 = index + 1 == intersections.Count ? intersections[index] : intersections[index + 1] };
                    triangle.MatchDirection(-slicePlane.normal);
                    newTriangles1.Add(triangle);
                }

                for (int index = 0; index < intersections.Count; index++)
                {
                    Triangle triangle = new Triangle() { v1 = intersections[index], v2 = center, v3 = index + 1 == intersections.Count ? intersections[index] : intersections[index + 1] };
                    triangle.MatchDirection(slicePlane.normal);
                    newTriangles2.Add(triangle);
                }
            }

            //This is where they put mouse button down but this is onTriggerExit so...?
            if (intersections.Count > 0)
            {
                //Get the original material
                Material material = other.gameObject.GetComponent<MeshRenderer>().material;

                //Don't need this unsliced gameobject piece of trash no sir
                Destroy(other.gameObject);

                Mesh mesh1 = new Mesh();
                Mesh mesh2 = new Mesh();

                List<Vector3> tris = new List<Vector3>();
                List<int> indices = new List<int>();

                int index = 0;

                //Generate the first slice
                foreach (Triangle tri in newTriangles1)
                {
                    tris.Add(tri.v1);
                    tris.Add(tri.v2);
                    tris.Add(tri.v3);
                    indices.Add(index++);
                    indices.Add(index++);
                    indices.Add(index++);
                }
                mesh1.vertices = tris.ToArray();
                mesh1.triangles = indices.ToArray();

                index = 0;
                tris.Clear();
                indices.Clear();

                //Generate the second slice
                foreach (Triangle tri in newTriangles2)
                {
                    tris.Add(tri.v1);
                    tris.Add(tri.v2);
                    tris.Add(tri.v3);
                    indices.Add(index++);
                    indices.Add(index++);
                    indices.Add(index++);
                }
                mesh2.vertices = tris.ToArray();
                mesh2.triangles = indices.ToArray();

                mesh1.RecalculateNormals();
                mesh1.RecalculateBounds();
                mesh2.RecalculateNormals();
                mesh2.RecalculateBounds();

                //Create the slice objects
                GameObject slicedObject1 = new GameObject();
                GameObject slicedObject2 = new GameObject();

                //Add all appropriate components - slice 1
                MeshFilter meshFilter1 = slicedObject1.AddComponent<MeshFilter>();
                meshFilter1.mesh = mesh1;
                MeshRenderer meshRenderer1 = slicedObject1.AddComponent<MeshRenderer>();
                meshRenderer1.material = material;
                MeshCollider meshCollider1 = slicedObject1.AddComponent<MeshCollider>();
                meshCollider1.convex = true;
                slicedObject1.AddComponent<Rigidbody>();
                meshCollider1.sharedMesh = mesh1;
                //Making the sliced pieces sliceable
                slicedObject1.tag = "Sliceable";
                //This is a check for trees and objects residing on the ground - if one of the slices are in collision with the ground - keep it that way. Stop the trunks of trees from flying off
                slicedObject1.AddComponent<CheckSlicedObjPosition>();

                //Add all appropriate components - slice 2
                MeshFilter meshFilter2 = slicedObject2.AddComponent<MeshFilter>();
                meshFilter2.mesh = mesh2;
                MeshRenderer meshRenderer2 = slicedObject2.AddComponent<MeshRenderer>();
                meshRenderer2.material = material;
                MeshCollider meshCollider2 = slicedObject2.AddComponent<MeshCollider>();
                meshCollider2.convex = true;
                slicedObject2.AddComponent<Rigidbody>();
                meshCollider2.sharedMesh = mesh2;
                slicedObject2.tag = "Sliceable";
                slicedObject2.AddComponent<CheckSlicedObjPosition>();

                //Adding some force to make the slices move
                slicedObject1.GetComponent<Rigidbody>().AddExplosionForce(350f, colliderPosition, 1.0f, 200.0f);
            }
        }
    }
}
