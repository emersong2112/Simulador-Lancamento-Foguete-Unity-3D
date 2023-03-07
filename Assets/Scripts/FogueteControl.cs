using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class FogueteControl : MonoBehaviour
{
    [Header("Atributos Iniciais")]
    [Tooltip("Massa total do foguete (sem contar o combustível)")]
    public float mass;
    [Tooltip("Massa total do tanque (sem contar o combustível)")]
    public float tankMass;
    [Tooltip("Massa do combustivel com tanque cheio")]
    public float gasMass;

    [Tooltip("Tempo de duração do combustível")]
    public float time = 5;

    [Tooltip("Tempo para desacoplar (só é valido no modo automático")]
    public float uncopleTime = 5;

    [Tooltip("Forca do vento")]
    public float wind = 5000;

    [Tooltip("Forca do paraquedas")]
    public float parachuteDrag = 2;

    [Tooltip("Força gerada pelo motor")]
    public float motor1Force, motor2Force;

    [Header("Elementos UI")]
    public TMP_Text textTimeLeft;
    public TMP_Text textMass, heightMax, vel, impact, height;
    public TMP_InputField uiMass, uiGasMass, uiTime, uiMotor1Force, uiMotor2Force, uiTankMass, uiWindForce, uiParachuteDrag, uiUnclopeTime;
    public Animator canvasAnim;
    public Button btnUnlock, btnOpen;

    [Header("Partes do Foguete")]
    public Transform tankMesh;
    public Transform pointMotor1;
    public ParticleSystem fireMotor1;
    public Transform pointMotor2;
    public ParticleSystem fireMotor2;
    public Transform parachute;
    public Animator animAudio;
    public AudioSource parachuteAudio;
    public AudioSource collisionAudio;
    public Transform rotBall;

    private bool active = false, automatic = true, flag = false, ballistic=false;
    private float timeLeft, upForce;
    private Rigidbody rb;
    private Animator paraAnim;
    private ParticleSystem.EmissionModule em1, em2;
    private Quaternion resetRot;
    void Start()
    {
        timeLeft = time;
        resetRot = transform.rotation;
        rb = transform.GetComponent<Rigidbody>();

        rb.freezeRotation = true;
        uiMass.text = mass.ToString();
        uiGasMass.text = gasMass.ToString();
        uiTime.text = time.ToString();
        uiMotor1Force.text = motor1Force.ToString();
        uiMotor2Force.text = motor2Force.ToString();
        uiTankMass.text = tankMass.ToString();
        uiWindForce.text = wind.ToString();
        uiParachuteDrag.text = parachuteDrag.ToString();
        uiUnclopeTime.text = uncopleTime.ToString();
        textMass.text = rb.mass.ToString();
        em1 = fireMotor1.emission;
        em2 = fireMotor2.emission;
        em1.enabled = false;
        em2.enabled = false;
        paraAnim = parachute.GetComponent<Animator>();
        upForce = motor1Force;
        btnOpen.interactable = !automatic;
        btnUnlock.interactable = !automatic;
    }

    // Update is called once per frame
    void Update()
    {
        if (active)
        {
            timeLeft -= Time.deltaTime;

            if (timeLeft > 0)
            {
                rb.centerOfMass = transform.position;
                rb.mass = mass + tankMass + ((gasMass * timeLeft) / time); //Cálculo da massa do foguete relativa ao combustível restante
                rb.AddForce(calcForce());
                if (timeLeft <= (time-uncopleTime) && automatic) Uncouple();
            }
            else
            {

                em1.enabled = false;
                em2.enabled = false;
                active = false;
                timeLeft = 0;
                flag = true;
                animAudio.SetTrigger("Off");
            }

            textTimeLeft.text = timeLeft.ToString();
            textMass.text = rb.mass.ToString();

        }

        if (rb.velocity.y < 0 && flag)
        {
            if(automatic) OpenParachute();
            heightMax.text = transform.position.y.ToString();
            flag = false;
        }

        vel.text = rb.velocity.magnitude.ToString();
        height.text = (transform.position.y - 100).ToString(); //Menos 100 devido offset do terreno.

    }

    public void LaunchRocket()
    {
        if (!active)
        {
            rb.freezeRotation = false;
            mass = float.Parse(uiMass.text);
            gasMass = float.Parse(uiGasMass.text);
            time = float.Parse(uiTime.text);
            motor1Force = float.Parse(uiMotor1Force.text);
            motor2Force = float.Parse(uiMotor2Force.text);
            tankMass = float.Parse(uiTankMass.text);
            wind = float.Parse(uiWindForce.text);
            parachuteDrag = float.Parse(uiParachuteDrag.text);
            uncopleTime = float.Parse(uiUnclopeTime.text);
            timeLeft = time;
            em1.enabled = true;
            active = true;
            animAudio.SetTrigger("Launcher");
            canvasAnim.SetTrigger("Launch");
        }
    }

    public void IsAutomatic(Toggle i)
    {
        automatic = i.isOn;
        btnOpen.interactable = !automatic;
        btnUnlock.interactable = !automatic;
    }
    public void IsBallistic(Toggle i)
    {
        ballistic = i.isOn;
        if (ballistic) transform.rotation = rotBall.rotation;
        else transform.rotation = resetRot;
    }

    public void Uncouple()
    {
        tankMesh.parent = null;
        Rigidbody rbTank = tankMesh.GetComponent<Rigidbody>();
        rbTank.isKinematic = false;
        rbTank.velocity = new Vector3(rb.velocity.x, rb.velocity.y, rb.velocity.z); //Não passei o objeto diretamente para não manter a referência e um não influenciar o outro.
        rb.mass -= tankMass;
        rbTank.mass = tankMass;
        em1.enabled = false;
        if(timeLeft > 0) em2.enabled = true;
        upForce = motor2Force;
        btnUnlock.interactable = false;
    }
    public void OpenParachute()
    {
        paraAnim.SetTrigger("open");
        parachuteAudio.Play();
        rb.drag = parachuteDrag;
        rb.centerOfMass = pointMotor2.position;
        btnOpen.interactable = false;
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Ground")
        {
            rb.drag = 0;
            Destroy(parachute.gameObject);
            rb.ResetCenterOfMass();
            collisionAudio.Play();

            float now = (collision.impulse / Time.fixedDeltaTime).magnitude;
            if (now > float.Parse(impact.text)) impact.text = now.ToString();
        }
    }

    private Vector3 calcForce()
    {
        return ((ballistic ? (Vector3.up + (Vector3.right/2f)) : Vector3.up) * upForce * Time.deltaTime) + (Vector3.forward * Random.Range(wind - (wind / 4), wind) * Time.deltaTime);
    }

    public void Restart()
    {
        SceneManager.LoadScene(0);
    }

    public void Quit()
    {
        Application.Quit();
    }
}