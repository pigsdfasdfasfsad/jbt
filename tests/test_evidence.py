import hashlib,pytest
from conftest import ROOT,load
A=load('evidence/recovery/source_archive_verification.json')['archives']
@pytest.mark.parametrize('rec',A)
def test_recovery_time_source_archive_hash(rec): assert rec['match'] is True and rec['actual_sha256']==rec['expected_sha256'] and len(rec['actual_sha256'])==64
def test_six_source_archives(): assert len(A)==6 and load('evidence/recovery/source_archive_verification.json')['all_match'] is True
def test_historical_checkpoint_qualified():
 d=load('evidence/recovery/historical_checkpoint.json'); assert d['historical_validation']['tests_passed']==82; assert 'Historical engineering-report evidence only' in d['qualification']
def test_historical_remotes_count_not_current_claim():
 d=load('content/remotes/recovery_status.json'); assert d['historical_runtime_normalization']['reported_calls']==14363 and d['historical_runtime_normalization']['current_runtime_call_count'] is None
def test_evidence_catalog_counts():
 d=load('content/recovery/source_counts.json'); assert d['source_archives_verified']==6 and d['source_files_verified']==8033 and d['distinct_hashes']==7698
def test_reward_analysis_source_confirmed_but_not_completion_reward():
 d=load('evidence/catalog/reward_analysis.json'); assert d['arithmetic_confidence']=='source-confirmed literal' and d['bases']['Regular']==1500
