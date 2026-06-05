import React from 'react';
import ConfirmModal from './ConfirmModal';
export default function FilterModal({ visible, onClose }: { visible: boolean; onClose: () => void }) { return <ConfirmModal visible={visible} title="Filtros" message="Filtros mobile serão aplicados conforme os endpoints paginados." onCancel={onClose} onConfirm={onClose} />; }
