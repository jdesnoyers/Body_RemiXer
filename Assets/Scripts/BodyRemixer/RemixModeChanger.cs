﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class RemixModeChanger : MonoBehaviour
{
    [SerializeField] private BodyRemixerController controller;
    [SerializeField] private float secondsPerMode = 120.0f;
    [SerializeField] private bool autoAdvance = false;

    Dictionary<RemixMode, RemixMode> nextRemixer = new Dictionary<RemixMode, RemixMode>()
    {
        {RemixMode.off, RemixMode.swap },
        {RemixMode.swap, RemixMode.average },
        {RemixMode.average, RemixMode.exquisite },
        {RemixMode.exquisite, RemixMode.shiva },
        {RemixMode.shiva, RemixMode.off }
    };

    Dictionary<RemixMode, RemixMode> prevRemixer = new Dictionary<RemixMode, RemixMode>()
    {
        {RemixMode.off ,RemixMode.shiva },
        { RemixMode.swap, RemixMode.off},
        {RemixMode.average, RemixMode.swap },
        {RemixMode.exquisite, RemixMode.average },
        {RemixMode.shiva, RemixMode.exquisite }
    };

    private float elapsedTime = 0.0f;
    private float lastTime = 0.0f;

    // Start is called before the first frame update
    void Start()
    {
        if(controller == null)
        {
            controller = GetComponent<BodyRemixerController>();
        }

    }

    // Update is called once per frame
    void Update()
    {

        if(autoAdvance)
        {
            elapsedTime = Time.time - lastTime;

            if(elapsedTime > secondsPerMode)
            {
                controller.remixMode = nextRemixer[controller.remixMode];
                lastTime = Time.time;
            }

        }

        if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus) || Input.GetKeyDown(KeyCode.Plus))
        {
            controller.remixMode = nextRemixer[controller.remixMode];
        }
        else if (Input.GetKeyDown(KeyCode.Underscore) || Input.GetKeyDown(KeyCode.KeypadMinus) || Input.GetKeyDown(KeyCode.Minus))
        {
            controller.remixMode = prevRemixer[controller.remixMode];

        }

        if (Input.GetKeyDown(KeyCode.LeftBracket))
        {
            autoAdvance = false;
        }

        if (Input.GetKeyDown(KeyCode.RightBracket))
        {
            autoAdvance = true;
        }


    }
}
