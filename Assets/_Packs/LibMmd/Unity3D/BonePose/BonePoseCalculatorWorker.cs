using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace LibMMD.Unity3D.BonePose
{
    public class BonePoseCalculatorWorker
    {
        private readonly HashSet<BonePosePreCalculator> _calculators = new HashSet<BonePosePreCalculator>();
        private volatile Thread _workThread;
        private volatile bool _isRunning = false;

        public BonePoseCalculatorWorker()
        {
            // 注册到资源管理器
            MmdResourceManager.RegisterWorker(this);
        }

        //返回id
        public void Start(BonePosePreCalculator calculator)
        {
            lock (this)
            {
                _calculators.Add(calculator);
                if (_workThread == null || !_workThread.IsAlive)
                {
                    _isRunning = true;
                    _workThread = new Thread(Run);
                    _workThread.IsBackground = true; // 确保线程是后台线程，不会阻止应用退出
                    _workThread.Start();
                }
            }
            lock (_calculators)
            {
                Monitor.PulseAll(_calculators);
            }
        }

        public void Stop(BonePosePreCalculator calculator)
        {
            lock (this)
            {
                _calculators.Remove(calculator);
            }
        }

        public void StopAllAndTerminate()
        {
            lock (this)
            {
                _calculators.Clear();
                _isRunning = false;
            }
            
            lock (_calculators)
            {
                Monitor.PulseAll(_calculators);
            }
            
            // 等待线程结束，但设置超时以防止无限阻塞
            if (_workThread != null && _workThread.IsAlive)
            {
                var joinSuccess = _workThread.Join(1000); // 等待最多1秒
                if (!joinSuccess)
                {
                    Debug.LogWarning("BonePoseCalculatorWorker thread did not terminate in time.");
                    // 在生产环境中应避免Abort，但在这种情况下可能是必要的
                    try
                    {
                        _workThread.Abort();
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError("Failed to abort worker thread: " + e.Message);
                    }
                }
                _workThread = null;
            }
        }

        private void Run()
        {
            while (_isRunning)
            {
                var shouldContinue = false;
                List<BonePosePreCalculator> calculators;
                lock (this)
                {
                    if (!_isRunning) break;
                    calculators = new List<BonePosePreCalculator>(_calculators);
                }
                foreach (var calculator in calculators)
                {
                    if (calculator.Step())
                    {
                        shouldContinue = true;
                    }
                }
                if (shouldContinue) continue;
                lock (_calculators)
                {
                    if (!_isRunning) break;
                    Monitor.Wait(_calculators, 100); // 添加超时，避免永久阻塞
                }
            }        }

        public void NotifyTake(BonePosePreCalculator calculator)
        {
            lock (_calculators)
            {
                Monitor.PulseAll(_calculators);
            }
        }
    }
}