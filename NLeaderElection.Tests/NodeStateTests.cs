using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace NLeaderElection.Tests
{
    [TestFixture]
    public class NodeStateTests
    {

        # region follower test cases

        [Test]
        public void TestIfFollowerIsRespondingToRequestVotesFromCandidate()
        {

        }

        [Test]
        public void TestIfFollowerIsRespondingToHeartBeatTimeoutFromLeader()
        {
            Follower follower = new FollowerStub();
            Leader leader = new Leader();
            leader.RegisterFollower(follower);
            Thread.Sleep(500);

            int expectedCount = 3;
            int outputCount = ((FollowerStub)follower).GetHeartbeatSingalCounts();

            Assert.AreEqual(expectedCount, outputCount);
            
        }

        [Test]
        public void TestIfFollowerIsConvertingToCandidateWithoutReceivingAppendEntries()
        {

        }

        [Test]
        public void TestIfFollowerIsConvertingToCandidateWithoutGrantingVoteToCandidate()
        {

        }

        # endregion 

        # region Candidate Test Cases

        [Test]
        public void TestIfCandidateVotesForSelf()
        {

        }

        [Test]
        public void TestIfCandidateResetTimeoutAfterVotingForSelf()
        {

        }

        [Test]
        public void TestIfCandidateSendRequestVotesToAllServers()
        {

        }

        [Test]
        public void TestIfCandidateResetElectionzTimeout()
        {

        }

        [Test]
        public void TestIfCandidateStepDownAfterReceivingAppendEntriesFromNewLeader()
        {

        }

        [Test]
        public void TestIfCandidateBecomeLeaderAfterWinningQuorum()
        {

        }

        [Test]
        public void TestIfCandidateStartANewElectionWhenTimoutWithoutElectionResolution()
        {

        }

        [Test]
        public void TestIfCandidateStepDownAfterDiscoveringHigherTerm()
        {

        }
        
        # endregion

        # region Leader test cases

        [Test]
        public void TestIfLeaderSetsNextIndexToNextValue()
        {
        }

        [Test]
        public void TestIfLeaderSendsHeartBeatAppendEntriesToEachFollower()
        {
        }

        [Test]
        public void TestIfLeaderAcceptCommandFromClients()
        {
        }

        [Test]
        public void TestIfLeaderAppendLogEntriesToLocalLog()
        {
        }

        [Test]
        public void TestIfLeaderStepDownWhenCurrentTermChanges()
        {
        }

        [Test]
        public void TestIfLeaderDecrementNextIndexOnLogInconsistency()
        {
        }

        # endregion
    }
}
