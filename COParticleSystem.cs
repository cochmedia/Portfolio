using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class COParticleSystem : MonoBehaviour {

    public int MaxParticles = 500000;
    public float EmitRadius = 10;
    public float Dampling = 1;
    public Gradient StartColor;
    public Vector2 StartSize = new Vector2(0.1f,1);
    public float StartSpeed = 4.0f;
    public Vector2 StartRotation = new Vector2(0.1f,1);
    public Vector2 StartRotationVelocity = new Vector2(-10, 10);
    public Texture2D Texture;

    [Range(0.0f,1.0f)]
    public float Alpha = 1;
    [Range(0.0f,1.0f)]
    public float SizeMultiply = 1;

    [Range(0,0.1f)]
    public float TurbulenceForceStrength;
    [Range(0, 0.1f)]
    public float GraphicForceStrength;
    [Range(-0.1f, 0.1f)]
    public float VortexForceStrength;

    public Vector3 VortexForceDirection;

    [Range(-0.01f, 0.01f)]
    public float AttractForceStrength;
    [Range(-0.2f, 0.2f)]
    public float DirectionalForceStrength;

    public Vector3 DirectionalForceDirection;

    public Material RenderMaterial;
    private ComputeShader particleCalculation;

    private const int c_groupSize = 128;
    private int m_updateParticlesKernel;

    //define the same structure as computer shader
    struct Particles
    {
        public Vector3 position;
        public float rotation;
        public float rotationVelocity;
        public Vector3 velocity;
        public Vector4 color;
        public float startSize;
        public float size;
        public Vector3 seed;
    }

    //define the same structure as computer shader
    public struct GraphicTargetData
    {
        public Vector3 position;
        public Vector4 color;
    }

    private ComputeBuffer m_particlesBuffer;
    private const int c_particleStride = 68;

    private static ComputeBuffer m_quadPoints;
    private const int c_quadStride = 12;

    private ComputeBuffer m_targetBuffer;
    private const int c_targetStride = 28;

    private int numberOfGroups;

    private bool isWorking = false;

    public void UpdateGraphic(GraphicTargetData[] data)
    {
        m_targetBuffer.SetData(data);
        particleCalculation.SetBuffer(m_updateParticlesKernel, "targetInfos", m_targetBuffer);
    }

    //provide a bound that all particles will move to the inside of it
    public void SetParticlePosition(Bounds bound)
    {
        //Debug.Log(bound);
        Particles[] ps = new Particles[MaxParticles];
        m_particlesBuffer.GetData(ps);

        for (int i = 0; i < ps.Length; i++)
        {
            ps[i].position = new Vector3(Random.Range(bound.min.x, bound.max.x), Random.Range(bound.min.y, bound.max.y), Random.Range(bound.min.z, bound.max.z));
            ps[i].velocity = Vector3.zero;
        }

        m_particlesBuffer.SetData(ps);
    }

    //provide a circle that all particles will move to the inside of it
    public void SetParticlePosition(Vector3 position,float radius)
    {
        Particles[] ps = new Particles[MaxParticles];
        m_particlesBuffer.GetData(ps);
        m_particlesBuffer.SetData(ps);
    }

    //change the texture of each single particle
    public void SetParticleTexture(Texture2D texture)
    {
        RenderMaterial.mainTexture = texture;
    }

    //offset the position of all particles
    public void Offset(Vector3 offset)
    {
        ComputeBuffer offsetBuff = new ComputeBuffer(1, 12);
        offsetBuff.SetData(new Vector3[] { offset });
        particleCalculation.SetBuffer(m_updateParticlesKernel,"offsetBuff", offsetBuff);
        particleCalculation.Dispatch(m_updateParticlesKernel, numberOfGroups, 1, 1);
    }

    public void Play()
    {
        //init particle buff
        m_particlesBuffer = new ComputeBuffer(MaxParticles, c_particleStride);

        Particles[] ps = new Particles[MaxParticles];

        for (int i = 0; i < MaxParticles; i++)
        {
            ps[i].position = Random.insideUnitSphere * EmitRadius;
            ps[i].rotation = Random.Range(StartRotation.x, StartRotation.y);
            ps[i].rotationVelocity = Random.Range(StartRotationVelocity.x, StartRotationVelocity.y);
            ps[i].velocity = Random.insideUnitSphere * StartSpeed;
            ps[i].color = StartColor.Evaluate(Random.Range(0.0f, 1.0f));
            ps[i].startSize = Random.Range(StartSize.x, StartSize.y);
            ps[i].size = ps[i].startSize;
            ps[i].seed = new Vector3(Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), Random.Range(0.5f, 1.5f));

        }

        m_particlesBuffer.SetData(ps);

        particleCalculation.SetBuffer(m_updateParticlesKernel, "particles", m_particlesBuffer);

        //init particle target
        m_targetBuffer = new ComputeBuffer(MaxParticles, c_targetStride);

        //set particle buff
        particleCalculation.SetBuffer(m_updateParticlesKernel, "targetInfos", m_targetBuffer);

        numberOfGroups = Mathf.CeilToInt((float)MaxParticles / c_groupSize);

        isWorking = true;
    }

    // Use this for initialization
    void Awake () {

        RenderMaterial = new Material(Shader.Find("Coch/COParticleSolid"));
        SetParticleTexture(Texture);

        particleCalculation = (ComputeShader)Instantiate(Resources.Load<ComputeShader>("Compute/COParticleSystem"));

        m_updateParticlesKernel = particleCalculation.FindKernel("UpdateParticle");

        //create a rect shape for particle
        if (m_quadPoints == null)
        {
            m_quadPoints = new ComputeBuffer(6, c_quadStride);

            m_quadPoints.SetData(new[]
            {
                new Vector3(-0.5f,0.5f),
                new Vector3 (0.5f,0.5f),
                new Vector3(0.5f,-0.5f),
                new Vector3(0.5f,-0.5f),
                new Vector3(-0.5f,-0.5f),
                new Vector3(-0.5f,0.5f)
            });
        }
    }

    // Update particle's parameters
    void Update () {

        if (!isWorking) return;

        particleCalculation.SetFloat("runtime", Time.realtimeSinceStartup);
        particleCalculation.SetFloat("deltaTime", Time.deltaTime);

        particleCalculation.SetFloat("dampling", Dampling);

        particleCalculation.SetFloat("directionalForceDirectionX", DirectionalForceDirection.x);
        particleCalculation.SetFloat("directionalForceDirectionY", DirectionalForceDirection.y);
        particleCalculation.SetFloat("directionalForceDirectionZ", DirectionalForceDirection.z);

        particleCalculation.SetFloat("vortexForceDirectionX", VortexForceDirection.x);
        particleCalculation.SetFloat("vortexForceDirectionY", VortexForceDirection.y);
        particleCalculation.SetFloat("vortexForceDirectionZ", VortexForceDirection.z);

        particleCalculation.SetFloat("graphicForceStrength", GraphicForceStrength );
        particleCalculation.SetFloat("vortexForceStrength", VortexForceStrength );
        particleCalculation.SetFloat("turbulenceForceStrength", TurbulenceForceStrength );
        particleCalculation.SetFloat("attractForceStrength", AttractForceStrength );
        particleCalculation.SetFloat("directionalForceStrength", DirectionalForceStrength );

        particleCalculation.SetFloat("alpha", Alpha);
        particleCalculation.SetFloat("sizeMultiply", SizeMultiply);

        particleCalculation.Dispatch(m_updateParticlesKernel, numberOfGroups, 1, 1);
    }

    private void OnRenderObject()
    {
        if (!isWorking) return;

        RenderMaterial.SetBuffer("particles", m_particlesBuffer);
        RenderMaterial.SetBuffer("quadPoints", m_quadPoints);
        RenderMaterial.SetVector("transformPosition", transform.position);

        RenderMaterial.SetPass(0);

        Graphics.DrawProcedural(MeshTopology.Triangles, 6, MaxParticles);
    }

    private void OnDestroy()
    {
        if (m_targetBuffer != null) m_targetBuffer.Dispose();
        if (m_particlesBuffer != null) m_particlesBuffer.Dispose();
    }
}
