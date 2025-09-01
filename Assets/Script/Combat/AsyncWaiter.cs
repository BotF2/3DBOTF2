using System;
using System.Threading.Tasks;
using UnityEngine;

public class AsyncWaiter
{
    private volatile bool _isProcessed = false;

    public async Task WaitAsync()
    {
        Console.WriteLine("Waiting for boolean to be true...");
        while (!_isProcessed)
        {
            // Delay without blocking the thread.
            await Task.Delay(100);
        }
        Console.WriteLine("Boolean is now true. Continuing...");
    }

    public void SetProcessed()
    {
        _isProcessed = true;
    }
}
