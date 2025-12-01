
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import Modal, { ConfirmDialog } from '../Modal';

describe('Modal Component', () => {
  it('renders modal when isOpen is true', () => {
    render(
      <Modal isOpen={true} onClose={() => {}}>
        <p>Test content</p>
      </Modal>
    );

    expect(screen.getByText('Test content')).toBeInTheDocument();
    expect(screen.getByRole('dialog')).toBeInTheDocument();
  });

  it('does not render when isOpen is false', () => {
    render(
      <Modal isOpen={false} onClose={() => {}}>
        <p>Test content</p>
      </Modal>
    );

    expect(screen.queryByText('Test content')).not.toBeInTheDocument();
  });

  it('displays title when provided', () => {
    render(
      <Modal isOpen={true} onClose={() => {}} title="Test Modal">
        <p>Content</p>
      </Modal>
    );

    expect(screen.getByText('Test Modal')).toBeInTheDocument();
  });

  it('closes modal when close button is clicked', async () => {
    const user = userEvent.setup();
    const onClose = jest.fn();

    render(
      <Modal isOpen={true} onClose={onClose} title="Test Modal">
        <p>Content</p>
      </Modal>
    );

    const closeButton = screen.getByLabelText('Закрыть модальное окно');
    await user.click(closeButton);

    expect(onClose).toHaveBeenCalledTimes(1);
  });

  it('closes modal when backdrop is clicked', async () => {
    const user = userEvent.setup();
    const onClose = jest.fn();

    render(
      <Modal isOpen={true} onClose={onClose} closeOnBackdropClick={true}>
        <p>Content</p>
      </Modal>
    );

    // Клик на backdrop (вне модального контента)
    const backdrop = screen.getByRole('dialog');
    await user.click(backdrop);

    expect(onClose).toHaveBeenCalledTimes(1);
  });

  it('does not close when clicking inside modal content', async () => {
    const user = userEvent.setup();
    const onClose = jest.fn();

    render(
      <Modal isOpen={true} onClose={onClose}>
        <p>Content</p>
      </Modal>
    );

    const modalContent = screen.getByText('Content');
    await user.click(modalContent);

    expect(onClose).not.toHaveBeenCalled();
  });

  it('closes modal on Escape key press', async () => {
    const user = userEvent.setup();
    const onClose = jest.fn();

    render(
      <Modal isOpen={true} onClose={onClose} closeOnEscape={true}>
        <p>Content</p>
      </Modal>
    );

    await user.keyboard('{Escape}');

    expect(onClose).toHaveBeenCalledTimes(1);
  });

  it('does not close on Escape when closeOnEscape is false', async () => {
    const user = userEvent.setup();
    const onClose = jest.fn();

    render(
      <Modal isOpen={true} onClose={onClose} closeOnEscape={false}>
        <p>Content</p>
      </Modal>
    );

    await user.keyboard('{Escape}');

    expect(onClose).not.toHaveBeenCalled();
  });

  it('renders footer when provided', () => {
    const footerContent = <div>Footer content</div>;

    render(
      <Modal isOpen={true} onClose={() => {}} footer={footerContent}>
        <p>Content</p>
      </Modal>
    );

    expect(screen.getByText('Footer content')).toBeInTheDocument();
  });

  it('applies correct size classes', () => {
    const { rerender } = render(
      <Modal isOpen={true} onClose={() => {}} size="sm">
        <p>Content</p>
      </Modal>
    );

    expect(screen.getByRole('dialog').firstElementChild).toHaveClass('max-w-md');

    rerender(
      <Modal isOpen={true} onClose={() => {}} size="lg">
        <p>Content</p>
      </Modal>
    );

    expect(screen.getByRole('dialog').firstElementChild).toHaveClass('max-w-2xl');

    rerender(
      <Modal isOpen={true} onClose={() => {}} size="full">
        <p>Content</p>
      </Modal>
    );

    expect(screen.getByRole('dialog').firstElementChild).toHaveClass('max-w-full');
  });

  it('hides close button when showCloseButton is false', () => {
    render(
      <Modal isOpen={true} onClose={() => {}} showCloseButton={false} title="Test">
        <p>Content</p>
      </Modal>
    );

    expect(screen.queryByLabelText('Закрыть модальное окно')).not.toBeInTheDocument();
  });

  it('applies custom className', () => {
    render(
      <Modal isOpen={true} onClose={() => {}} className="custom-class">
        <p>Content</p>
      </Modal>
    );

    const modalContent = screen.getByRole('dialog').firstElementChild;
    expect(modalContent).toHaveClass('custom-class');
  });

  it('manages body scroll lock', () => {
    // Перед тестом сохраняем оригинальное значение
    const originalOverflow = document.body.style.overflow;

    const { unmount } = render(
      <Modal isOpen={true} onClose={() => {}}>
        <p>Content</p>
      </Modal>
    );

    expect(document.body.style.overflow).toBe('hidden');

    unmount();

    expect(document.body.style.overflow).toBe('unset');

    // Восстанавливаем оригинальное значение
    document.body.style.overflow = originalOverflow;
  });

  it('focuses first focusable element when opened', async () => {
    render(
      <Modal isOpen={true} onClose={() => {}}>
        <p>Content</p>
      </Modal>
    );

    await waitFor(() => {
      const closeButton = screen.getByLabelText('Закрыть модальное окно');
      expect(closeButton).toHaveFocus();
    });
  });

  it('supports accessibility attributes', () => {
    render(
      <Modal isOpen={true} onClose={() => {}} title="Accessible Modal">
        <p>Content</p>
      </Modal>
    );

    const dialog = screen.getByRole('dialog');
    expect(dialog).toHaveAttribute('aria-modal', 'true');
    expect(dialog).toHaveAttribute('aria-labelledby', 'modal-title');
  });

  it('handles keyboard navigation', async () => {
    const user = userEvent.setup();

    render(
      <Modal isOpen={true} onClose={() => {}}>
        <button>Button 1</button>
        <button>Button 2</button>
      </Modal>
    );

    // Фокус уже на close button (первом фокусируемом элементе)
    const closeButton = screen.getByLabelText('Закрыть модальное окно');
    expect(closeButton).toHaveFocus();

    // Tab navigation внутри модального окна
    await user.tab(); // Фокус на Button 1
    expect(screen.getByText('Button 1')).toHaveFocus();

    await user.tab(); // Фокус на Button 2
    expect(screen.getByText('Button 2')).toHaveFocus();
  });

  it('prevents event bubbling when clicking modal content', async () => {
    const user = userEvent.setup();
    const onBackdropClick = jest.fn();

    // Создаем mock backdrop click handler
    const TestModal = () => (
      <div onClick={onBackdropClick}>
        <Modal isOpen={true} onClose={() => {}}>
          <button onClick={(e) => e.stopPropagation()}>Click me</button>
        </Modal>
      </div>
    );

    render(<TestModal />);

    const button = screen.getByText('Click me');
    await user.click(button);

    // Backdrop click не должен быть вызван
    expect(onBackdropClick).not.toHaveBeenCalled();
  });
});

describe('ConfirmDialog Component', () => {
  it('renders confirmation dialog with title and message', () => {
    render(
      <ConfirmDialog
        isOpen={true}
        onClose={() => {}}
        onConfirm={() => {}}
        title="Подтверждение"
        message="Вы уверены?"
      />
    );

    expect(screen.getByText('Подтверждение')).toBeInTheDocument();
    expect(screen.getByText('Вы уверены?')).toBeInTheDocument();
    expect(screen.getByText('Подтвердить')).toBeInTheDocument();
    expect(screen.getByText('Отмена')).toBeInTheDocument();
  });

  it('calls onConfirm and closes when confirm button is clicked', async () => {
    const user = userEvent.setup();
    const onClose = jest.fn();
    const onConfirm = jest.fn();

    render(
      <ConfirmDialog
        isOpen={true}
        onClose={onClose}
        onConfirm={onConfirm}
        title="Test"
        message="Message"
      />
    );

    const confirmButton = screen.getByText('Подтвердить');
    await user.click(confirmButton);

    expect(onConfirm).toHaveBeenCalledTimes(1);
    expect(onClose).toHaveBeenCalledTimes(1);
  });

  it('closes dialog when cancel button is clicked', async () => {
    const user = userEvent.setup();
    const onClose = jest.fn();
    const onConfirm = jest.fn();

    render(
      <ConfirmDialog
        isOpen={true}
        onClose={onClose}
        onConfirm={onConfirm}
        title="Test"
        message="Message"
      />
    );

    const cancelButton = screen.getByText('Отмена');
    await user.click(cancelButton);

    expect(onConfirm).not.toHaveBeenCalled();
    expect(onClose).toHaveBeenCalledTimes(1);
  });

  it('uses custom button texts', () => {
    render(
      <ConfirmDialog
        isOpen={true}
        onClose={() => {}}
        onConfirm={() => {}}
        title="Test"
        message="Message"
        confirmText="Да"
        cancelText="Нет"
      />
    );

    expect(screen.getByText('Да')).toBeInTheDocument();
    expect(screen.getByText('Нет')).toBeInTheDocument();
  });

  it('applies danger styling for confirm button', () => {
    render(
      <ConfirmDialog
        isOpen={true}
        onClose={() => {}}
        onConfirm={() => {}}
        title="Test"
        message="Message"
        confirmButtonVariant="danger"
      />
    );

    const confirmButton = screen.getByText('Подтвердить');
    expect(confirmButton).toHaveClass('bg-red-600');
  });

  it('shows loading state and disables buttons', () => {
    render(
      <ConfirmDialog
        isOpen={true}
        onClose={() => {}}
        onConfirm={() => {}}
        title="Test"
        message="Message"
        loading={true}
      />
    );

    expect(screen.getByText('Загрузка...')).toBeInTheDocument();

    const confirmButton = screen.getByText('Загрузка...');
    const cancelButton = screen.getByText('Отмена');

    expect(confirmButton).toBeDisabled();
    expect(cancelButton).toBeDisabled();
  });

  it('focuses primary action button in confirm dialog', async () => {
    render(
      <ConfirmDialog
        isOpen={true}
        onClose={() => {}}
        onConfirm={() => {}}
        title="Test"
        message="Message"
      />
    );

    // Проверяем, что confirm button (primary action) получает фокус
    const confirmButton = screen.getByText('Подтвердить');
    await waitFor(() => {
      expect(confirmButton).toHaveFocus();
    });
  });

  it('supports screen readers with proper ARIA attributes', () => {
    render(
      <ConfirmDialog
        isOpen={true}
        onClose={() => {}}
        onConfirm={() => {}}
        title="Удаление данных"
        message="Это действие нельзя отменить"
      />
    );

    const dialog = screen.getByRole('dialog');
    expect(dialog).toHaveAttribute('aria-modal', 'true');
    expect(dialog).toHaveAttribute('aria-labelledby', 'modal-title');

    const confirmButton = screen.getByText('Подтвердить');
    expect(confirmButton).toHaveAttribute('type', 'button');
  });

  it('maintains focus management', async () => {
    const user = userEvent.setup();

    render(
      <>
        <button>External button</button>
        <ConfirmDialog
          isOpen={true}
          onClose={() => {}}
          onConfirm={() => {}}
          title="Test"
          message="Message"
        />
      </>
    );

    // Фокус должен оставаться внутри модального окна
    await user.tab();
    await user.tab();
    await user.tab();

    // Не должен вернуться к внешней кнопке
    const externalButton = screen.getByText('External button');
    expect(externalButton).not.toHaveFocus();
  });

  it('handles single button click', async () => {
    const user = userEvent.setup();
    const onConfirm = jest.fn();
    const onClose = jest.fn();

    render(
      <ConfirmDialog
        isOpen={true}
        onClose={onClose}
        onConfirm={onConfirm}
        title="Test"
        message="Message"
      />
    );

    const confirmButton = screen.getByText('Подтвердить');

    // Один клик должен вызвать onConfirm и onClose
    await user.click(confirmButton);
    expect(onConfirm).toHaveBeenCalledTimes(1);
    expect(onClose).toHaveBeenCalledTimes(1);
  });
});
