import React from 'react';
import InputField from './InputField';
export default function DatePickerField({ label, value }: { label: string; value?: string }) { return <InputField label={label} value={value ?? ''} editable={false} />; }
