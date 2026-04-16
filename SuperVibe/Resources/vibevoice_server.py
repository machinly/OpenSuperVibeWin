"""
Persistent VibeVoice-ASR server for SuperVibe Windows.
Loads the Microsoft VibeVoice-ASR model once at startup.
Reads WAV file paths from stdin (one per line),
transcribes, and writes JSON results to stdout.

Usage: python vibevoice_server.py [model_path]
"""
import sys
import os
import json
import re
import torch


def main():
    model_path = sys.argv[1] if len(sys.argv) > 1 else "microsoft/VibeVoice-ASR"

    # Store models in project models/ directory if available
    script_dir = os.path.dirname(os.path.abspath(__file__))
    project_root = os.path.normpath(os.path.join(script_dir, "..", "..", "..", ".."))
    models_dir = os.path.join(project_root, "models")
    if os.path.isdir(os.path.dirname(models_dir)):
        os.makedirs(models_dir, exist_ok=True)
        os.environ["HF_HOME"] = models_dir

    sys.stderr.write(f"[vibevoice] Loading model {model_path}...\n")
    sys.stderr.flush()

    from vibevoice.modular.modeling_vibevoice_asr import VibeVoiceASRForConditionalGeneration
    from vibevoice.processor.vibevoice_asr_processor import VibeVoiceASRProcessor

    processor = VibeVoiceASRProcessor.from_pretrained(
        model_path,
        language_model_pretrained_name="Qwen/Qwen2.5-7B",
    )

    model = VibeVoiceASRForConditionalGeneration.from_pretrained(
        model_path,
        dtype=torch.bfloat16,
        attn_implementation="sdpa",
        trust_remote_code=True,
    )
    model.eval()

    if torch.cuda.is_available():
        model = model.to("cuda")
        sys.stderr.write("[vibevoice] Using CUDA\n")
    else:
        sys.stderr.write("[vibevoice] Using CPU (slow)\n")

    sys.stderr.write("[vibevoice] Model loaded. Ready.\n")
    sys.stderr.flush()

    # Signal ready
    sys.stdout.write("READY\n")
    sys.stdout.flush()

    for line in sys.stdin:
        audio_path = line.strip()
        if not audio_path:
            continue

        try:
            inputs = processor(
                audio=audio_path,
                return_tensors="pt",
                padding=True,
                add_generation_prompt=True,
            )
            inputs = {
                k: v.to(model.device) if isinstance(v, torch.Tensor) else v
                for k, v in inputs.items()
            }

            with torch.no_grad():
                output_ids = model.generate(
                    **inputs,
                    max_new_tokens=512,
                    pad_token_id=processor.pad_id,
                    eos_token_id=processor.tokenizer.eos_token_id,
                )

            raw_text = processor.decode(output_ids[0], skip_special_tokens=True)

            # Strip timestamps like <|0.00|> and speaker labels like Speaker1:
            text = re.sub(r"<\|[\d.]+\|>", "", raw_text)
            text = re.sub(r"Speaker\d+:\s*", "", text)
            text = text.strip()

            sys.stdout.write(json.dumps({"ok": True, "text": text}) + "\n")
        except Exception as e:
            sys.stdout.write(json.dumps({"ok": False, "error": str(e)}) + "\n")

        sys.stdout.flush()


if __name__ == "__main__":
    main()
