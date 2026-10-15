from importlib.util import module_from_spec, spec_from_file_location
from pathlib import Path
import pytest

spec = spec_from_file_location('matrix_runner', Path(__file__).parents[1] / 'tools/validation/run_runtime_matrix.py')
runner = module_from_spec(spec)
spec.loader.exec_module(runner)

@pytest.mark.parametrize('expression,expected', [
    (None, True), (False, False), ('false && success()', False),
    ('success() && (true || false)', True),
    ("github.ref_name == 'twr-pass123-physical-objective-routes'", True),
    ("github.ref_name == 'retired-pass'", False),
    ("success() && (github.ref_name == 'other' || github.ref_name == 'twr-pass123-physical-objective-routes')", True),
    ('${{ !false && always() }}', True),
])
def test_conditions(expression, expected):
    assert runner.condition(expression) is expected

@pytest.mark.parametrize('expression', [
    "unknown()", "true || unknown()", "false && unknown()",
    "__import__('os').system('anything')", "github.secret == 'x'", "1+2",
])
def test_unknown_conditions_fail_closed(expression):
    with pytest.raises((ValueError, SyntaxError)):
        runner.condition(expression)


def workflow():
    names = ['Native Pass 119 fixture', 'Native Pass 120 fixture', 'Native Pass 121 fixture',
             'Native Pass 123 fixture', 'Smoke Pass 90 fixture', 'Smoke Pass 92 fixture',
             'Smoke Pass 94 fixture', 'Verify no private map scenes']
    names += [f'Other native regression {i}' for i in range(25)]
    return {'jobs': {'validate-and-export': {'steps': [
        {'name': 'Setup', 'uses': 'actions/setup-python@v6'},
        {'name': 'Verify executable', 'run': 'check exe', 'shell': 'pwsh'},
        *[{'name': name, 'run': f"Write-Host '{name}'", 'shell': 'pwsh', 'if': 'success()'} for name in names],
        {'name': 'Disabled artifact', 'uses': 'actions/upload-artifact@v4', 'if': 'false && success()'},
    ]}}}


def test_complete_matrix_keeps_original_bodies_and_checks():
    value = workflow()
    selected = runner.select_steps(value)
    original = value['jobs']['validate-and-export']['steps'][2:-1]
    assert selected == original
    assert len(selected) == 33


def test_focused_checks_are_additional_not_a_replacement():
    selected = runner.select_steps(workflow(), focus=True)
    assert [s['name'] for s in selected] == ['Smoke Pass 90 fixture', 'Smoke Pass 92 fixture', 'Smoke Pass 94 fixture']


def test_missing_privacy_guard_is_an_error():
    value = workflow()
    value['jobs']['validate-and-export']['steps'] = [
        s for s in value['jobs']['validate-and-export']['steps'] if not s['name'].startswith('Verify no private map')]
    with pytest.raises(ValueError):
        runner.select_steps(value)


def test_enabled_artifact_is_never_silently_skipped():
    value = workflow()
    value['jobs']['validate-and-export']['steps'][-1]['if'] = True
    with pytest.raises(ValueError):
        runner.select_steps(value)
