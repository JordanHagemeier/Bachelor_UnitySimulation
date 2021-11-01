using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComputeShaderCSharpTest : MonoBehaviour
{

    public struct Cube
    {
        public Vector3 position;
        public Color color;
    }

    public ComputeShader computeShader;
    public RenderTexture renderTexture;

    private Cube[] data;
    // Start is called before the first frame update
    void Start()
    {
        renderTexture = new RenderTexture(256, 256, 24);
        renderTexture.enableRandomWrite = true;
        renderTexture.Create();

        computeShader.SetTexture(0, "Result", renderTexture);
        computeShader.Dispatch(0, renderTexture.width / 8, renderTexture.height / 8, 1);
    }


    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if(renderTexture == null)
        {
            renderTexture = new RenderTexture(256, 256, 24);
            renderTexture.enableRandomWrite = true;
            renderTexture.Create();
        }
        computeShader.SetTexture(0, "Result", renderTexture);
        computeShader.SetFloat("Resolution", renderTexture.width);
        computeShader.Dispatch(0, renderTexture.width / 8, renderTexture.height / 8, 1);

        Graphics.Blit(renderTexture, destination);

    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
