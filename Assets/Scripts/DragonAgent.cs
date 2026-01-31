using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class DragonAgent : Agent
{
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Animator animator;

    [SerializeField] private float wingFlapForce = 3000f;
    [SerializeField] private float turnTorque = 60f;
    [SerializeField] private float speedForce = 2000f;
    [SerializeField] private float maxSpeed = 25f;

    [SerializeField] private Transform target;
    [SerializeField] private DragonTrainingEnvironment environment;

    [SerializeField] private string flapSpeedParam = "FlapSpeed";
    [SerializeField] private string flySpeedParam = "FlySpeed";
    [SerializeField] private bool useAnimator = false;

    private float episodeStartTime;
    private float totalDistanceTraveled;
    private Vector3 lastPosition;
    private float smoothnessScore;
    private int obstacleHits;
    private bool reachedTarget;
    private string currentSessionID;

    private Vector3 startPosition;
    private Quaternion startRotation;
    private float episodeTimer;
    private float lastDistanceToTarget;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();

        rb.useGravity = true;
        rb.linearDamping = 1.5f;
        rb.angularDamping = 6f;
        rb.mass = 50f;
        rb.constraints = RigidbodyConstraints.None;

        startPosition = transform.localPosition;
        startRotation = transform.localRotation;

        if (environment == null)
        {
            environment = GetComponentInParent<DragonTrainingEnvironment>();
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (animator != null)
        {
            useAnimator = true;
            Debug.Log("Animator found - will control animation parameters");
        }
        currentSessionID = "Dragon_Session_" + System.DateTime.Now.ToString("MMdd_HHmm");
    }

    public override void OnEpisodeBegin()
    {
        transform.localPosition = startPosition;
        transform.localRotation = startRotation;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        episodeTimer = 0f;
        episodeStartTime = Time.time;
        totalDistanceTraveled = 0f;
        lastPosition = transform.localPosition;
        smoothnessScore = 1f;
        obstacleHits = 0;
        reachedTarget = false;

        if (target != null)
        {
            lastDistanceToTarget = Vector3.Distance(transform.localPosition, target.localPosition);
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (target == null) return;

        Vector3 toTarget = target.localPosition - transform.localPosition;
        float distanceToTarget = toTarget.magnitude;

        sensor.AddObservation(transform.localPosition / 50f);
        sensor.AddObservation(transform.forward);
        sensor.AddObservation(transform.up);
        sensor.AddObservation(rb.linearVelocity / 20f);
        sensor.AddObservation(rb.linearVelocity.magnitude / 20f);

        sensor.AddObservation(toTarget / 50f);
        sensor.AddObservation(distanceToTarget / 50f);
        sensor.AddObservation(toTarget.normalized);

        sensor.AddObservation(Vector3.Dot(transform.forward, toTarget.normalized));
        sensor.AddObservation(Vector3.Dot(rb.linearVelocity.normalized, toTarget.normalized));
        sensor.AddObservation((target.localPosition.y - transform.localPosition.y) / 20f);
        sensor.AddObservation(transform.up.y);

        if (environment != null)
        {
            Vector3 avoidanceDir = environment.GetNearestObstacleDirection(transform.localPosition, 15f);
            sensor.AddObservation(avoidanceDir);
        }
        else
        {
            sensor.AddObservation(Vector3.zero);
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float leftWingFlap = Mathf.Clamp(actions.ContinuousActions[0], 0f, 1f);
        float rightWingFlap = Mathf.Clamp(actions.ContinuousActions[1], 0f, 1f);
        float speedControl = Mathf.Clamp(actions.ContinuousActions[2], 0f, 1f);

        ApplyDragonPhysics(leftWingFlap, rightWingFlap, speedControl);
        UpdateAnimations(leftWingFlap, rightWingFlap, speedControl);
        CalculateRewards();
        TrackPerformance();

        episodeTimer += Time.fixedDeltaTime;
        CheckTerminationConditions();
    }

    private void ApplyDragonPhysics(float leftWing, float rightWing, float speed)
    {
        if (rb.linearVelocity.magnitude > maxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }

        float averageFlap = (leftWing + rightWing) / 2f;
        float flapDifference = leftWing - rightWing;

        Vector3 liftForce = transform.up * averageFlap * wingFlapForce;
        rb.AddForce(liftForce, ForceMode.Force);

        float turnAmount = flapDifference * turnTorque;
        rb.AddTorque(Vector3.up * turnAmount, ForceMode.Acceleration);

        Vector3 forwardForce = transform.forward * speed * speedForce;
        rb.AddForce(forwardForce, ForceMode.Force);

        float currentRoll = transform.localEulerAngles.z;
        if (currentRoll > 180) currentRoll -= 360;
        float rollCorrection = -currentRoll * 3.0f;
        rb.AddRelativeTorque(Vector3.forward * rollCorrection, ForceMode.Acceleration);

        float currentPitch = transform.localEulerAngles.x;
        if (currentPitch > 180) currentPitch -= 360;
        if (Mathf.Abs(currentPitch) > 60f)
        {
            float pitchCorrection = -currentPitch * 1.5f;
            rb.AddRelativeTorque(Vector3.right * pitchCorrection, ForceMode.Acceleration);
        }
    }

    private void UpdateAnimations(float leftFlap, float rightFlap, float speed)
    {
        if (!useAnimator || animator == null) return;

        float averageFlap = (leftFlap + rightFlap) / 2f;

        if (animator.parameters != null)
        {
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                if (param.name == flapSpeedParam)
                {
                    animator.SetFloat(flapSpeedParam, averageFlap);
                }

                if (param.name == flySpeedParam)
                {
                    animator.SetFloat(flySpeedParam, speed);
                }
            }
        }
    }

    private void CalculateRewards()
    {
        if (target == null) return;

        float currentDistance = Vector3.Distance(transform.localPosition, target.localPosition);
        Vector3 toTarget = (target.localPosition - transform.localPosition).normalized;

        float distanceReward = (lastDistanceToTarget - currentDistance) * 3.0f;
        AddReward(distanceReward);
        lastDistanceToTarget = currentDistance;

        float proximityBonus = Mathf.Exp(-currentDistance / 10f) * 0.01f;
        AddReward(proximityBonus);

        float facingDot = Vector3.Dot(transform.forward, toTarget);
        if (facingDot > 0.3f)
        {
            AddReward(facingDot * 0.008f);
        }

        if (rb.linearVelocity.magnitude > 0.1f)
        {
            float velocityAlignment = Vector3.Dot(rb.linearVelocity.normalized, toTarget);
            AddReward(velocityAlignment * 0.012f);
        }

        AddReward(transform.up.y * 0.003f);

        float speedRatio = rb.linearVelocity.magnitude / maxSpeed;
        if (speedRatio > 0.3f && speedRatio < 0.8f)
        {
            AddReward(0.005f);
        }

        if (environment != null && environment.CheckCollision(transform.localPosition, 2f))
        {
            AddReward(-0.05f);
            obstacleHits++;
        }

        float angularSpeed = rb.angularVelocity.magnitude;
        if (angularSpeed > 5f)
        {
            AddReward(-0.003f * angularSpeed);
        }

        AddReward(-0.0005f);
    }

    private void TrackPerformance()
    {
        float distanceDelta = Vector3.Distance(transform.localPosition, lastPosition);
        totalDistanceTraveled += distanceDelta;
        lastPosition = transform.localPosition;

        float velocityChange = (rb.linearVelocity - rb.linearVelocity).magnitude;
        smoothnessScore *= 0.99f;
        smoothnessScore += (1f - Mathf.Clamp01(velocityChange / 10f)) * 0.01f;
    }

    private void CheckTerminationConditions()
    {
        if (target == null) return;

        float currentDistance = Vector3.Distance(transform.localPosition, target.localPosition);

        if (currentDistance < 5f)
        {
            AddReward(100f);
            reachedTarget = true;
            SavePerformanceData(true);
            EndEpisode();
        }

        if (transform.localPosition.y < -5f)
        {
            AddReward(-20f);
            SavePerformanceData(false);
            EndEpisode();
        }

        if (currentDistance > 100f || transform.localPosition.y > 60f)
        {
            AddReward(-10f);
            SavePerformanceData(false);
            EndEpisode();
        }

        if (episodeTimer > 60f)
        {
            AddReward(-5f);
            SavePerformanceData(false);
            EndEpisode();
        }

        if (transform.up.y < -0.5f)
        {
            AddReward(-8f);
            SavePerformanceData(false);
            EndEpisode();
        }
    }

    private void SavePerformanceData(bool success)
    {
        NeuralNetworkSaver saver = FindFirstObjectByType<NeuralNetworkSaver>();
        if (saver != null)
        {
            float episodeTime = Time.time - episodeStartTime;
            float efficiency = totalDistanceTraveled > 0 ? lastDistanceToTarget / totalDistanceTraveled : 0f;

            saver.TrackNetworkPerformance(
                currentSessionID,
                GetCumulativeReward(),
                success,
                episodeTime,
                smoothnessScore
            );
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActions = actionsOut.ContinuousActions;

        continuousActions[0] = Input.GetKey(KeyCode.Q) ? 1f : 0f;
        continuousActions[1] = Input.GetKey(KeyCode.E) ? 1f : 0f;
        continuousActions[2] = Input.GetKey(KeyCode.Space) ? 1f : 0f;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Obstacle"))
        {
            AddReward(-2f);
            obstacleHits++;
        }
    }

    public float GetSmoothness() => smoothnessScore;
    public int GetObstacleHits() => obstacleHits;
    public bool GetReachedTarget() => reachedTarget;
}