using System.Media;
using System.Threading;
using System.Threading.Tasks;
using CoolFishNS.Utilities;
using NLog;

namespace CoolFishNS.Bots.FiniteStateMachine.States
{
    /// <summary>
    ///     This state is run if we want to be notified by whispers in the game and one occurs.
    /// </summary>
    public class StateDoWhisper : State
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public override int Priority
        {
            get { return (int) CoolFishEngine.StatePriority.StateDoWhisper; }
        }

        /// <summary>
        ///     Get the message and author of the whisper and display it to the user and play a sound.
        /// </summary>
        public override bool Run()
        {
            if (!UserPreferences.Default.SoundOnWhisper)
            {
                return false;
            }
            if (Manager.GetGlobalVariable("NewMessage") == "1")
            {
                Manager.ExecuteScript("NewMessage = 0;");
                var message = Manager.GetGlobalVariable("Message");
                var author = Manager.GetGlobalVariable("Author");

                Logger.Info("Whisper from: " + author + " Message: " + message);

                Task.Run(() =>
                {
                    for (var i = 0; i < 2; i++)
                    {
                        SystemSounds.Hand.Play();
                        Thread.Sleep(3000);
                    }
                });
                return true;
            }
            return false;
        }
    }
}