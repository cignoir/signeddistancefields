﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

//main signed distance fiedl test component
[ExecuteInEditMode]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class SignedDistanceField : MonoBehaviour
{
    //render mode
    public enum Mode
    {
        Black,
        RawTexture,
        Distance,
        Gradient,
        Solid,
        Border,
        SolidWithBorder
    }

    //shader to use
    public Shader m_sdf_shader;

    //render options
    public Mode m_mode = Mode.SolidWithBorder;
    public Texture2D m_texture;
    public bool m_show_grid = false;
    public FilterMode m_filter = FilterMode.Bilinear;
    public float m_text_grid_size = 40f;
    public bool m_show_text = false;
    public Color m_background = new Color32(0x13,0x13,0x80,0xFF);
    public Color m_fill = new Color32(0x7E,0x16,0x16,0xFF);
    public Color m_border = new Color32(0xD2,0x17,0x17,0xFF);
    public float m_border_width = 0.5f;
    public float m_offset = 0f;
    public float m_distance_visualisation_scale = 1f;

    //internally created temp material
    Material m_material;

    //OnRenderObject calls init, then sets up render parameters
    public void OnRenderObject()
    {
        //make sure we have all the bits needed for rendering
        if (!m_texture)
        {
            m_texture = Texture2D.whiteTexture;
        }
        if (!m_material)
        {
            m_material = new Material(m_sdf_shader);
            m_material.hideFlags = HideFlags.DontSave;
            GetComponent<MeshRenderer>().sharedMaterial = m_material;
            GetComponent<MeshFilter>().sharedMesh = BuildQuad(Vector2.one);
        }

        //store texture filter mode
        m_texture.filterMode = m_filter;
        m_texture.wrapMode = TextureWrapMode.Clamp;

        //store material properties
        m_material.SetTexture("_MainTex", m_texture);
        m_material.SetInt("_Mode", (int)m_mode);
        m_material.SetFloat("_BorderWidth", m_border_width);
        m_material.SetFloat("_Offset", m_offset);
        m_material.SetFloat("_Grid", m_show_grid ? 0.75f : 0f);
        m_material.SetColor("_Background", m_background);
        m_material.SetColor("_Fill", m_fill);
        m_material.SetColor("_Border", m_border);
        m_material.SetFloat("_DistanceVisualisationScale", m_distance_visualisation_scale);
    }

    //debug function for bodgily rendering a grid of pixel distances
    public void OnGUI()
    {
        if (m_show_text && m_texture)
        {
            Color[] pixels = m_texture.GetPixels();

            float sz = m_text_grid_size;
            Vector2 tl = new Vector2(Screen.width, Screen.height) * 0.5f - sz * new Vector2(m_texture.width, m_texture.height) * 0.5f;
            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.white;
            style.fontSize = 20;
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.MiddleCenter;
            for (int y = 0; y < m_texture.height; y++)
            {
                for (int x = 0; x < m_texture.width; x++)
                {
                    GUI.Label(new Rect(tl.x + x * sz, tl.y + y * sz, sz, sz), string.Format("{0:0.0}",pixels[m_texture.width*y+x].r, style));
                }
            }
        }
    }

    //helper to build a temporary quad with the correct winding + uvs
    static Mesh BuildQuad(Vector2 half_size)
    {
        var mesh = new Mesh();
        mesh.hideFlags = HideFlags.HideAndDontSave;

        var vertices = new Vector3[4];
        vertices[0] = new Vector3(-half_size.x, -half_size.y, 0);
        vertices[1] = new Vector3(half_size.x, -half_size.y, 0);
        vertices[2] = new Vector3(-half_size.x, half_size.y, 0);
        vertices[3] = new Vector3(half_size.x, half_size.y, 0);
        mesh.vertices = vertices;

        var tri = new int[6];
        tri[0] = 0;
        tri[1] = 1;
        tri[2] = 2;
        tri[3] = 2;
        tri[4] = 1;
        tri[5] = 3;
        mesh.triangles = tri;

        var normals = new Vector3[4];
        normals[0] = Vector3.forward;
        normals[1] = Vector3.forward;
        normals[2] = Vector3.forward;
        normals[3] = Vector3.forward;
        mesh.normals = normals;

        var uv = new Vector2[4];
        uv[0] = new Vector2(0, 0);
        uv[1] = new Vector2(1, 0);
        uv[2] = new Vector2(0, 1);
        uv[3] = new Vector2(1, 1);
        mesh.uv = uv;

        return mesh;
    }

}

//custom inspector 
#if UNITY_EDITOR
[CustomEditor(typeof(SignedDistanceField))]
public class SignedDisanceFieldEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawDefaultInspector();

        SignedDistanceField field = (SignedDistanceField)target;
        if (GUILayout.Button("Generate Centre Line"))
        {
            SignedDistanceFieldGenerator generator = new SignedDistanceFieldGenerator(16, 16);
            generator.BFLine(new Vector2(3.5f, 8.5f), new Vector2(12.5f, 8.5f));
            field.m_texture = generator.End();
        }
        if (GUILayout.Button("Generate 1 circle"))
        {
            SignedDistanceFieldGenerator generator = new SignedDistanceFieldGenerator(16, 16);
            generator.BFCircle(new Vector2(8, 8), 4);
            field.m_texture = generator.End();
        }
        if (GUILayout.Button("Generate 1 rectangle"))
        {
            SignedDistanceFieldGenerator generator = new SignedDistanceFieldGenerator(16, 16);
            generator.BFRect(new Vector2(3, 5), new Vector2(12,10));
            field.m_texture = generator.End();
        }
        if (GUILayout.Button("Generate 2 circles"))
        {
            SignedDistanceFieldGenerator generator = new SignedDistanceFieldGenerator(16, 16);
            generator.BFCircle(new Vector2(5, 7), 3);
            generator.BFCircle(new Vector2(10, 8), 3.5f);
            field.m_texture = generator.End();
        }
        if (GUILayout.Button("Generate 3 circles and a rect"))
        {
            SignedDistanceFieldGenerator generator = new SignedDistanceFieldGenerator(16, 16);
            generator.BFCircle(new Vector2(6, 6), 1.5f);
            generator.BFCircle(new Vector2(9, 10), 2.5f);
            generator.BFCircle(new Vector2(10, 6), 1.5f);
            generator.BFRect(new Vector2(3, 7), new Vector2(13, 10));
            field.m_texture = generator.End();
        }
        if (GUILayout.Button("Generate 3 circles and a rect hi"))
        {
            SignedDistanceFieldGenerator generator = new SignedDistanceFieldGenerator(64, 64);
            generator.BFCircle(new Vector2(6 * 4, 6 * 4), 1.5f * 4);
            generator.BFCircle(new Vector2(9 * 4, 10 * 4), 2.5f * 4);
            generator.BFCircle(new Vector2(10 * 4, 6 * 4), 1.5f * 4);
            generator.BFRect(new Vector2(3 * 4, 7 * 4), new Vector2(13 * 4, 10 * 4));
            field.m_texture = generator.End();
        }
        if (GUILayout.Button("Generate 2 close rectangles"))
        {
            SignedDistanceFieldGenerator generator = new SignedDistanceFieldGenerator(64, 64);
            generator.BFRect(new Vector2(4, 4), new Vector2(60, 35));
            generator.BFRect(new Vector2(4, 34), new Vector2(60, 60));
            field.m_texture = generator.End();
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif