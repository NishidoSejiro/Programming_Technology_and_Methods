using System;

namespace BankingSystem
{
	// 1. Абстрактный базовый класс счета
	public abstract class BankAccount
	{
		// Поле баланса скрыто от прямого внешнего изменения (инкапсуляция),
		// но доступно для классов-наследников через protected
		protected decimal balance;

		// Свойство только для чтения баланса
		public decimal Balance => balance;

		// Конструктор базового класса
		public BankAccount(decimal initialBalance)
		{
			if (initialBalance < 0)
			{
				throw new ArgumentException("Начальный баланс не может быть отрицательным.");
			}
			balance = initialBalance;
		}

		// Обычный метод пополнения счета (общий для всех видов счетов)
		public virtual void Deposit(decimal amount)
		{
			if (amount <= 0)
			{
				Console.WriteLine("[Ошибка] Сумма пополнения должна быть больше 0.");
				return;
			}
			balance += amount;
			Console.WriteLine($"[Пополнение] Зачислено: {amount:C}. Текущий баланс: {balance:C}");
		}

		// Абстрактный метод снятия — у каждого типа счета своя логика
		public abstract void Withdraw(decimal amount);
	}

	// 2. Сберегательный счет (SavingsAccount)
	public class SavingsAccount : BankAccount
	{
		// Поле для хранения неснижаемого минимального остатка
		private readonly decimal minBalance;

		public SavingsAccount(decimal initialBalance, decimal minBalance) : base(initialBalance)
		{
			this.minBalance = minBalance;
		}

		// Реализация абстрактного метода снятия денег с проверкой остатка
		public override void Withdraw(decimal amount)
		{
			if (amount <= 0)
			{
				Console.WriteLine("[Ошибка] Сумма снятия должна быть больше 0.");
				return;
			}

			if (balance - amount < minBalance)
			{
				Console.WriteLine($"[Отклонено] Ошибка снятия! На сберегательном счете должен оставаться минимальный остаток не менее {minBalance:C}. Текущий баланс: {balance:C}");
			}
			else
			{
				balance -= amount;
				Console.WriteLine($"[Снятие] Со сберегательного счета снято: {amount:C}. Остаток: {balance:C}");
			}
		}
	}

	// 3. Расчетный счет (CheckingAccount)
	public class CheckingAccount : BankAccount
	{
		// Фиксированная плата (комиссия) за процедуру снятия
		private readonly decimal serviceFee;

		public CheckingAccount(decimal initialBalance, decimal serviceFee) : base(initialBalance)
		{
			this.serviceFee = serviceFee;
		}

		// Реализация абстрактного метода снятия денег с удержанием комиссии
		public override void Withdraw(decimal amount)
		{
			if (amount <= 0)
			{
				Console.WriteLine("[Ошибка] Сумма снятия должна быть больше 0.");
				return;
			}

			decimal totalDeduction = amount + serviceFee;

			if (balance < totalDeduction)
			{
				Console.WriteLine($"[Отклонено] Недостаточно средств для снятия {amount:C} с учетом комиссии {serviceFee:C}. Текущий баланс: {balance:C}");
			}
			else
			{
				balance -= totalDeduction;
				Console.WriteLine($"[Снятие] С расчетного счета снято: {amount:C} (комиссия: {serviceFee:C}). Остаток: {balance:C}");
			}
		}
	}

	class Program
	{
		static void Main(string[] args)
		{
			Console.OutputEncoding = System.Text.Encoding.UTF8;
			Console.WriteLine("=== СИСТЕМА УПРАВЛЕНИЯ БАНКОВСКИМИ СЧЕТАМИ ===\n");

			// 1. Тестируем Сберегательный счет (Старт: 5000, минимальный остаток: 1000)
			Console.WriteLine("--- 1. Демонстрация Сберегательного счета ---");
			SavingsAccount savings = new SavingsAccount(5000, 1000);
			savings.Deposit(1000);  // Баланс станет: 6000
			savings.Withdraw(4500); // Успех (Остаток: 1500 >= 1000)
			savings.Withdraw(1000); // Ошибка (Остаток стал бы 500 < 1000)
			Console.WriteLine();

			// 2. Тестируем Расчетный счет (Старт: 3000, комиссия снятия: 50)
			Console.WriteLine("--- 2. Демонстрация Расчетного счета ---");
			CheckingAccount checking = new CheckingAccount(3000, 50);
			checking.Deposit(500);   // Баланс станет: 3500
			checking.Withdraw(1000); // Снимет 1000 + 50 комиссии = 1050 (Остаток: 2450)
			checking.Withdraw(2500); // Ошибка (Нужно 2550, а на балансе всего 2450)

			Console.WriteLine("\n=== ТЕСТИРОВАНИЕ УСПЕШНО ЗАВЕРШЕНО ===");
		}
	}
}
