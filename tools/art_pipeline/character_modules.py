"""Validation helpers for visible editable character module PNGs."""

from tools.art_pipeline.source_audit import assert_character_sources_complete


def validate_character_modules(recipe):
    assert_character_sources_complete((recipe,))
    return True
